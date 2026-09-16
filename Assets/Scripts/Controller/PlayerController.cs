using System;
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

        [Header("インベントリ参照")]
        [SerializeField] private InventorySO inventory;

        [Header("魔法発射ポイント")]
        [SerializeField] private Transform castPoint;

        [Header("デバッグ表示 (現在値)")]
        [SerializeField] private float currentHp;

        [Header("装備中の魔法情報 (リアルタイムデバッグ)")]
        [SerializeField] private List<SpellDebugInfo> equippedSpellsDebug = new List<SpellDebugInfo>();

        // 外部（ItemPickup等）からインベントリを参照するためのプロパティ
        public InventorySO Inventory => inventory;

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        private CharacterController characterController;
        private BuffHandler buffHandler;
        private readonly Dictionary<int, float> cooldownTimers = new Dictionary<int, float>();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            buffHandler = GetComponent<BuffHandler>();
            currentHp = maxHp;

            if (inventory != null)
            {
                inventory.InitializeInventory();
            }
        }

        private void OnEnable()
        {
            if (buffHandler != null)
            {
                buffHandler.OnPoisonTick += HandlePoisonDamage;
            }
        }

        private void OnDisable()
        {
            if (buffHandler != null)
            {
                buffHandler.OnPoisonTick -= HandlePoisonDamage;
            }
        }

        private void Update()
        {
            HandleMovement();
            UpdateCooldowns();
            AutoCastSpells();
            UpdateDebugInfo();
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

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDir.magnitude >= 0.1f)
            {
                float speedMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.SpeedUp, BuffType.Slow) : 1f;
                float moveSpeed = baseMoveSpeed * speedMult;

                characterController.Move(inputDir * moveSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(inputDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
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

            for (int i = 0; i < inventory.spellSlots.Length; i++)
            {
                MagicData magic = inventory.spellSlots[i];
                if (magic == null) continue;

                if (cooldownTimers.TryGetValue(i, out float remaining) && remaining > 0f)
                {
                    continue;
                }

                CastSpell(i);
            }
        }

        // PlayerController.cs 内の CastSpell メソッドを以下のように更新

        public void CastSpell(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventory.spellSlots.Length) return;

            MagicData magic = inventory.spellSlots[slotIndex];
            if (magic == null || magic.projectilePrefab == null) return;

            cooldownTimers[slotIndex] = magic.cooldown;

            Vector3 originPos = castPoint != null ? castPoint.position : transform.position + Vector3.up * 1f;

            // 扇形発射に対応する移動タイプ判定
            bool isSpreadType = magic.movementType == MovementType.Spread ||
                                magic.movementType == MovementType.Split ||
                                magic.movementType == MovementType.Boomerang ||
                                magic.movementType == MovementType.Homing;

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
                        float atkMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.AttackUp) : 1f;
                        projectile.Setup(magic, rotation * Vector3.forward, atkMult);
                    }
                }
            }
            else
            {
                // 単発生成（Straight, Laser など）
                GameObject projObj = Instantiate(magic.projectilePrefab, originPos, transform.rotation);
                if (projObj.TryGetComponent<MagicProjectile>(out var projectile))
                {
                    float atkMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.AttackUp) : 1f;
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
                float totalCd = magic != null ? magic.cooldown : 1f;

                float progress = magic != null && totalCd > 0f ? 1f - (remaining / totalCd) : 1f;

                equippedSpellsDebug.Add(new SpellDebugInfo
                {
                    magicData = magic,
                    damage = magic != null ? magic.damage : 0f,
                    maxCooldown = magic != null ? magic.cooldown : 0f,
                    cooldownProgress = Mathf.Clamp01(progress)
                });
            }
        }

        public void TakeDamage(float damageAmount)
        {
            float defenseMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.DefenseUp) : 1f;
            float finalDamage = Mathf.Max(1f, damageAmount / defenseMult);

            currentHp -= finalDamage;
            Debug.Log($"[Player] {finalDamage} のダメージを受けた！ 残りHP: {currentHp}");

            if (currentHp <= 0)
            {
                currentHp = 0;
                OnDeath();
            }
        }

        private void HandlePoisonDamage(BuffType type, float damage)
        {
            TakeDamage(damage);
        }

        private void OnDeath()
        {
            Debug.Log("[Player] 死亡しました。");
        }
    }
}