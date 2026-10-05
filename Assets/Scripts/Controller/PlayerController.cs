using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicRogue
{
    public class CustomProgressBarAttribute : PropertyAttribute { }

#if UNITY_EDITOR
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

    [Serializable]
    public struct SpellDebugInfo
    {
        public MagicData magicData;
        public float damage;
        public float maxCooldown;
        [CustomProgressBar] public float cooldownProgress;
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(BuffHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("基礎ステータス")]
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float baseMoveSpeed = 5f;

        [Header("連射設定")]
        [SerializeField] private float duplicateSpellDelay = 0.15f;

        [Header("インベントリ・コンポーネント参照")]
        [SerializeField] private InventorySO inventory;
        [SerializeField] private Transform castPoint;
        [SerializeField] private Animator animator;
        [SerializeField] private DamageFlash damageFlash; // ★ 追加

        [Header("デバッグ表示")]
        [SerializeField] private float currentHp;
        [SerializeField] private List<SpellDebugInfo> equippedSpellsDebug = new List<SpellDebugInfo>();

        public InventorySO Inventory => inventory;
        public float CurrentHp => currentHp;

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

        private int currentCastingSlot = -1;
        private bool isInvincible = false;
        private bool isControlActive = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            buffHandler = GetComponent<BuffHandler>();
            targetLockSystem = GetComponent<TargetLockSystem>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (damageFlash == null) damageFlash = GetComponent<DamageFlash>(); // ★ 自動取得

            if (inventory != null)
            {
                inventory.InitializeInventory();
            }
        }

        private void Start()
        {
            if (GameSceneManager.Instance != null && buffHandler != null)
            {
                GameSceneManager.Instance.ApplyAllBuffsToPlayer(buffHandler);
            }

            currentHp = MaxHp;
        }

        private void Update()
        {
            if (!isControlActive) return;

            HandleMovement();
            UpdateCooldowns();
            AutoCastSpells();
            UpdateDebugInfo();
        }

        public void SetInvincible(bool state) => isInvincible = state;
        public void SetControlActive(bool state) => isControlActive = state;

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

                if (Camera.main != null)
                {
                    Vector3 camForward = Camera.main.transform.forward;
                    Vector3 camRight = Camera.main.transform.right;

                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    moveDirection = (camForward * rawInput.z) + (camRight * rawInput.x);
                }

                float speedMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.MoveSpeedUp, BuffType.MoveSpeedDown) : 1f;
                float moveSpeed = baseMoveSpeed * speedMult;

                Vector3 targetPosition = transform.position + (moveDirection * moveSpeed * Time.deltaTime);

                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    Vector3 correctMoveVector = hit.position - transform.position;
                    characterController.Move(correctMoveVector);
                }

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
            if (targetLockSystem == null || !targetLockSystem.IsLockedOn) return;

            if (inventory == null || inventory.spellSlots == null) return;

            int totalSlots = inventory.spellSlots.Length;
            int reducedSlots = (int)(buffHandler != null ? buffHandler.GetTotalValue(BuffType.MagicSlotReduction) : 0);
            int activeSlotCount = Mathf.Max(1, totalSlots - reducedSlots);

            for (int i = 0; i < activeSlotCount; i++)
            {
                MagicData magic = inventory.spellSlots[i];
                if (magic == null) continue;

                if (cooldownTimers.TryGetValue(i, out float remaining) && remaining > 0f) continue;
                if (pendingSpells.Contains(magic)) continue;

                StartCoroutine(CastSpellWithDelay(i, magic));
            }
        }

        private IEnumerator CastSpellWithDelay(int slotIndex, MagicData magic)
        {
            pendingSpells.Add(magic);

            CastSpell(slotIndex);

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

            if (animator != null)
            {
                currentCastingSlot = slotIndex;
                animator.SetTrigger("CastMagic");
            }
            else
            {
                ExecuteMagicCast(slotIndex);
            }
        }

        public void OnPlayerCastMagicAnimation()
        {
            if (currentCastingSlot >= 0)
            {
                ExecuteMagicCast(currentCastingSlot);
                currentCastingSlot = -1;
            }
        }

        private void ExecuteMagicCast(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventory.spellSlots.Length) return;

            MagicData magic = inventory.spellSlots[slotIndex];
            if (magic == null || magic.projectilePrefab == null) return;

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
            if (isInvincible) return;

            if (buffHandler != null && buffHandler.OnPlayerTakeDamage())
            {
                return; // 被弾キャンセル（聖なる守りなど）
            }

            float damageMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.DamageReceivedUp) : 1f;
            float finalDamage = Mathf.Max(1f, damageAmount * damageMult);

            currentHp -= finalDamage;

            // ★ ダメージ点滅演出を再生
            if (damageFlash != null)
            {
                damageFlash.CallDamageFlash();
            }

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