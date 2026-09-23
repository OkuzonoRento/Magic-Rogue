using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicRogue
{
    // カスタムプログレスバー属性
    public class CustomProgressBarAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    // インスペクター上にプログレスバーを描画するエディター拡張
    [CustomPropertyDrawer(typeof(CustomProgressBarAttribute))]
    public class CustomProgressBarDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
                float value = Mathf.Clamp01(property.floatValue);
                Rect barPosition = EditorGUI.PrefixLabel(position, label);
                EditorGUI.ProgressBar(barPosition, value, $"{Mathf.RoundToInt(value * 100)}%");
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
#endif

    // インスペクター表示用の構造体
    [Serializable]
    public struct SpellDebugInfo
    {
        [Tooltip("装備中の魔法データ")]
        public MagicData magicData;

        [Tooltip("攻撃力")]
        public float damage;

        [Tooltip("最大クールタイム（秒）")]
        public float maxCooldown;

        [CustomProgressBar]
        public float cooldownProgress;
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(BuffHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("基礎ステータス")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float baseMoveSpeed = 5f;

        [Header("連射設定")]
        [Tooltip("同じ魔法を複数装備している場合の発射間隔（秒）")]
        [SerializeField] private float duplicateSpellDelay = 0.15f;

        [Header("インベントリ参照")]
        [SerializeField] private InventorySO inventory;

        [Header("魔法発射ポイント")]
        [SerializeField] private Transform castPoint;

        [Header("デバッグ表示 (現在値)")]
        [SerializeField] private float currentHp;

        [Header("装備中の魔法情報 (リアルタイムデバッグ)")]
        [SerializeField] private List<SpellDebugInfo> equippedSpellsDebug = new List<SpellDebugInfo>();

        public InventorySO Inventory => inventory;
        public float CurrentHp => currentHp;

        // 最大HP（MaxHpUp バフを反映）
        public float MaxHp
        {
            get
            {
                float mult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.MaxHpUp) : 1f;
                return maxHp * mult;
            }
        }

        private CharacterController characterController;
        private BuffHandler buffHandler;
        private TargetLockSystem targetLockSystem;
        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();
        private readonly HashSet<MagicData> pendingSpells = new HashSet<MagicData>();

        // シーン遷移演出用の制御フラグ
        private bool isInvincible = false;
        private bool isControlActive = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            buffHandler = GetComponent<BuffHandler>();
            targetLockSystem = GetComponent<TargetLockSystem>();

            if (inventory != null)
            {
                inventory.InitializeInventory();
            }
        }

        private void Start()
        {
            // 1. 出撃前画面で選択されたバフを適用
            if (GameSceneManager.Instance != null && buffHandler != null)
            {
                GameSceneManager.Instance.ApplyAllBuffsToPlayer(buffHandler);
            }

            // 2. バフ計算が完了したあとの MaxHp で currentHp を初期化する
            currentHp = MaxHp;
        }

        private void Update()
        {
            // 操作不能（魔法陣を踏んだ後の吸い寄せ中など）時は移動・攻撃の入力をスキップ
            if (!isControlActive) return;

            HandleMovement();
            UpdateCooldowns();
            AutoCastSpells();
            UpdateDebugInfo();
        }

        /// <summary>
        /// 無敵状態を設定する
        /// </summary>
        public void SetInvincible(bool state)
        {
            isInvincible = state;
            Debug.Log($"[Player] 無敵状態: {isInvincible}");
        }

        /// <summary>
        /// プレイヤーの操作可能状態を設定する（false でキー移動・自動攻撃を停止）
        /// </summary>
        public void SetControlActive(bool state)
        {
            isControlActive = state;
        }

        private void HandleMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.wKey?.isPressed == true || keyboard.upArrowKey?.isPressed == true) vertical += 1f;
            if (keyboard.sKey?.isPressed == true || keyboard.downArrowKey?.isPressed == true) vertical -= 1f;
            if (keyboard.dKey?.isPressed == true || keyboard.rightArrowKey?.isPressed == true) horizontal += 1f;
            if (keyboard.aKey?.isPressed == true || keyboard.leftArrowKey?.isPressed == true) horizontal -= 1f;

            Vector3 rawInput = new Vector3(horizontal, 0f, vertical).normalized;

            if (rawInput.magnitude >= 0.1f)
            {
                Vector3 moveDirection = rawInput;

                // メインカメラの向きに合わせて移動ベクトルを計算
                if (Camera.main != null)
                {
                    Vector3 camForward = Camera.main.transform.forward;
                    Vector3 camRight = Camera.main.transform.right;

                    // 高さを無視して平面ベクトル化
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    // カメラ視点に基づいた移動方向の算出
                    moveDirection = (camForward * rawInput.z) + (camRight * rawInput.x);
                }

                float moveSpeed = baseMoveSpeed;
                characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

                // ロックオン中でない場合のみ、移動入力の方向へ向きを変える
                if (targetLockSystem == null || !targetLockSystem.IsLockedOn)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                }
            }
        }

        private void UpdateCooldowns()
        {
            List<int> keys = new List<int>(cooldownTimers.Keys);
            foreach (int key in keys)
            {
                if (cooldownTimers[key] > 0f)
                {
                    cooldownTimers[key] -= Time.deltaTime;
                }
            }
        }

        private void AutoCastSpells()
        {
            if (inventory == null || inventory.spellSlots == null) return;

            int totalSlots = inventory.spellSlots.Length;
            int reducedSlots = (int)(buffHandler != null ? buffHandler.GetTotalValue(BuffType.MagicSlotReduction) : 0);
            int activeSlotCount = Mathf.Max(1, totalSlots - reducedSlots);

            for (int i = 0; i < activeSlotCount; i++)
            {
                MagicData magic = inventory.spellSlots[i];
                if (magic == null) continue;

                // クールタイム中の場合はスキップ
                if (cooldownTimers.TryGetValue(i, out float remaining) && remaining > 0f)
                {
                    continue;
                }

                // すでに同種魔法のディレイ発射待ち（Pending）ならスキップ
                if (pendingSpells.Contains(magic))
                {
                    continue;
                }

                // ディレイをつけて順次発射するコルーチンを実行
                StartCoroutine(CastSpellWithDelay(i, magic));
            }
        }

        private IEnumerator CastSpellWithDelay(int slotIndex, MagicData magic)
        {
            pendingSpells.Add(magic);

            CastSpell(slotIndex);

            // 連続発射用のテンポ（初期値0.15秒）だけ待機
            yield return new WaitForSeconds(duplicateSpellDelay);

            pendingSpells.Remove(magic);
        }

        public void CastSpell(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventory.spellSlots.Length) return;

            MagicData magic = inventory.spellSlots[slotIndex];
            if (magic == null || magic.projectilePrefab == null) return;

            float cdMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.CooldownIncrease, BuffType.CooldownReduction) : 1f;
            cooldownTimers[slotIndex] = magic.cooldown * cdMult;

            if (buffHandler != null && buffHandler.CheckIsFailed())
            {
                Debug.Log("[Player] 魔法の発動に失敗した！");
                return;
            }

            if (buffHandler != null)
            {
                float selfDamage = buffHandler.GetTotalValue(BuffType.SelfDamageOnAttack);
                if (selfDamage > 0f)
                {
                    TakeDamage(selfDamage);
                }
            }

            Vector3 originPos = castPoint != null ? castPoint.position : transform.position + Vector3.up * 1f;

            bool isSpreadType = magic.movementType == MovementType.Spread ||
                                magic.movementType == MovementType.Split ||
                                magic.movementType == MovementType.Boomerang ||
                                magic.movementType == MovementType.Homing;

            float atkMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.AttackUp, BuffType.AttackDown) : 1f;

            if (isSpreadType && magic.projectileCount > 1)
            {
                float startAngle = -magic.spreadAngle / 2f;
                float angleStep = magic.spreadAngle / (magic.projectileCount - 1);

                for (int i = 0; i < magic.projectileCount; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);
                    Quaternion rotation = transform.rotation * Quaternion.Euler(0f, currentAngle, 0f);

                    GameObject projObj = Instantiate(magic.projectilePrefab, originPos, rotation);
                    if (projObj.TryGetComponent<MagicProjectile>(out var projectile))
                    {
                        projectile.Setup(magic, rotation * Vector3.forward, atkMult);
                    }
                }
            }
            else
            {
                GameObject projObj = Instantiate(magic.projectilePrefab, originPos, transform.rotation);
                if (projObj.TryGetComponent<MagicProjectile>(out var projectile))
                {
                    projectile.Setup(magic, transform.forward, atkMult);
                }
            }

            if (magic.castEffectPrefab != null) Instantiate(magic.castEffectPrefab, originPos, transform.rotation);
            if (magic.castSound != null) AudioSource.PlayClipAtPoint(magic.castSound, originPos);
        }

        private void UpdateDebugInfo()
        {
            if (inventory == null || inventory.spellSlots == null) return;

            equippedSpellsDebug.Clear();

            for (int i = 0; i < inventory.spellSlots.Length; i++)
            {
                MagicData magic = inventory.spellSlots[i];
                float remaining = cooldownTimers.TryGetValue(i, out float timer) ? Mathf.Max(0f, timer) : 0f;
                float cdMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.CooldownIncrease, BuffType.CooldownReduction) : 1f;
                float totalCd = magic != null ? magic.cooldown * cdMult : 1f;

                float progress = magic != null && totalCd > 0f ? 1f - (remaining / totalCd) : 1f;

                equippedSpellsDebug.Add(new SpellDebugInfo
                {
                    magicData = magic,
                    damage = magic != null ? magic.damage * (buffHandler != null ? buffHandler.GetMultiplier(BuffType.AttackUp, BuffType.AttackDown) : 1f) : 0f,
                    maxCooldown = totalCd,
                    cooldownProgress = Mathf.Clamp01(progress)
                });
            }
        }

        public void TakeDamage(float damageAmount)
        {
            // 無敵状態ならダメージを受けない
            if (isInvincible) return;

            float damageMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.DamageReceivedUp) : 1f;
            float finalDamage = Mathf.Max(1f, damageAmount * damageMult);

            currentHp -= finalDamage;
            Debug.Log($"[Player] {finalDamage} のダメージを受けた！ 残りHP: {currentHp}");

            if (currentHp <= 0)
            {
                currentHp = 0;
                OnDeath();
            }
        }

        private void OnDeath()
        {
            Debug.Log("[Player] 死亡しました。");
        }
    }
}