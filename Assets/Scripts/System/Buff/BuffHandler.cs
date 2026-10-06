using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public class BuffHandler : MonoBehaviour
    {
        private readonly List<BuffInstance> activeBuffs = new List<BuffInstance>();

        private PlayerController player;
        private Vector3 lastPosition;
        private float stationaryTimer = 0f;
        private float elapsedTime = 0f;

        private int currentKills = 0;
        private bool isAscended = false;

        private bool isShieldReady = true;
        private float shieldCooldownTimer = 0f;

        private int comboHits = 0;
        private float comboTimer = 0f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            lastPosition = transform.position;

            if (HasBuff(BuffType.PathToAscension))
            {
                AddBuff(BuffType.MagicSlotReduction, 99f, 99999f);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.duration -= deltaTime;

                if (buff.duration <= 0f)
                {
                    activeBuffs.RemoveAt(i);
                }
            }

            float moveDist = Vector3.Distance(transform.position, lastPosition);
            if (moveDist < 0.01f) stationaryTimer += deltaTime;
            else stationaryTimer = 0f;
            lastPosition = transform.position;

            if (!isShieldReady)
            {
                shieldCooldownTimer -= deltaTime;
                if (shieldCooldownTimer <= 0f) isShieldReady = true;
            }

            if (comboHits > 0)
            {
                comboTimer -= deltaTime;
                if (comboTimer <= 0f) comboHits = 0;
            }
        }

        public void AddBuff(BuffType type, float value, float duration)
        {
            activeBuffs.Add(new BuffInstance(type, value, duration));
        }

        public float GetMultiplier(BuffType positiveBuff, BuffType negativeBuff = (BuffType)(-1))
        {
            float multiplier = 1.0f;

            foreach (var buff in activeBuffs)
            {
                if (buff.type == positiveBuff) multiplier += buff.value;
                else if (negativeBuff >= 0 && buff.type == negativeBuff) multiplier -= buff.value;
            }

            // 【攻撃力】
            if (positiveBuff == BuffType.AttackUp)
            {
                if (HasBuff(BuffType.StationaryTurret) && stationaryTimer >= 1.5f) multiplier += 0.4f;

                float moveDist = Vector3.Distance(transform.position, lastPosition);
                if (HasBuff(BuffType.Stepper) && moveDist >= 0.01f) multiplier += 0.2f;

                if (HasBuff(BuffType.Overload))
                {
                    if (elapsedTime <= 20f) multiplier += 0.5f;
                    else multiplier -= 0.3f;
                }

                if (HasBuff(BuffType.NoonPower))
                {
                    float progress = Mathf.Clamp01(elapsedTime / 180f);
                    if (progress >= 0.2f && progress <= 0.8f) multiplier += 0.5f;
                    else multiplier -= 0.3f;
                }

                if (HasBuff(BuffType.LastStand) && player != null && (player.CurrentHp / player.MaxHp) <= 0.2f)
                {
                    multiplier += 0.8f;
                }

                if (HasBuff(BuffType.FrogInAWell) && player != null)
                {
                    if (IsAllSlotsSame()) multiplier += 0.5f;
                    else multiplier -= 0.6f;
                }

                if (HasBuff(BuffType.PathToAscension))
                {
                    if (isAscended) multiplier += 2.0f;
                    else multiplier -= 0.5f;
                }

                if (HasBuff(BuffType.FleshCut)) multiplier += 0.3f;
            }

            // 【移動速度】
            if (positiveBuff == BuffType.MoveSpeedUp)
            {
                if (HasBuff(BuffType.EnemyStatDown)) multiplier -= 0.15f; // 敵ステータス低下等の影響
                if (HasBuff(BuffType.EnemyStatUp)) multiplier += 0.2f;
            }

            // 【クールタイム】
            if (positiveBuff == BuffType.CooldownIncrease || positiveBuff == BuffType.CooldownReduction)
            {
                if (positiveBuff == BuffType.CooldownReduction && HasBuff(BuffType.Overload) && elapsedTime <= 20f) multiplier += 0.3f;

                if (HasBuff(BuffType.FrogInAWell))
                {
                    if (IsAllSlotsSame() && positiveBuff == BuffType.CooldownReduction) multiplier += 0.3f;
                    else if (!IsAllSlotsSame() && positiveBuff == BuffType.CooldownIncrease) multiplier += 1.0f;
                }

                if (positiveBuff == BuffType.CooldownReduction && HasBuff(BuffType.MagicCirculation))
                {
                    multiplier += comboHits * 0.05f;
                }

                if (positiveBuff == BuffType.CooldownIncrease && HasBuff(BuffType.FleshCut)) multiplier += 0.2f;
            }

            // 【被ダメージ】
            if (positiveBuff == BuffType.DamageReceivedUp)
            {
                if (HasBuff(BuffType.FleshCut)) multiplier += 0.3f;
            }

            return Mathf.Max(0.1f, multiplier);
        }

        public float GetTotalValue(BuffType type)
        {
            float total = 0f;
            foreach (var buff in activeBuffs)
            {
                if (buff.type == type) total += buff.value;
            }
            return total;
        }

        public bool CheckIsFailed()
        {
            float failChance = GetTotalValue(BuffType.ChanceToFail);
            if (failChance <= 0f) return false;
            return UnityEngine.Random.value < failChance;
        }

        public bool HasBuff(BuffType type) => activeBuffs.Exists(b => b.type == type);
        public void ClearBuffsOfType(BuffType type) => activeBuffs.RemoveAll(b => b.type == type);

        #region イベント連動（敵へのバフ・デバフ伝播など）

        /// <summary>
        /// 魔法が敵に命中した際に呼び出され、敵へデバフ（呪いの杖等）を付与
        /// </summary>
        public void OnMagicHitEnemy(GameObject targetEnemy)
        {
            if (HasBuff(BuffType.MagicCirculation))
            {
                comboHits++;
                comboTimer = 2.5f;
            }

            // 呪術師の杖（敵のステータス低下・被ダメージ増を敵側BuffHandlerに付与）
            if (HasBuff(BuffType.CurseStaff) && targetEnemy != null)
            {
                if (targetEnemy.TryGetComponent<BuffHandler>(out var enemyBuffs))
                {
                    enemyBuffs.AddBuff(BuffType.DamageReceivedUp, 0.2f, 5f);
                    enemyBuffs.AddBuff(BuffType.MoveSpeedDown, 0.2f, 5f);
                    Debug.Log($"[呪いの杖] {targetEnemy.name} にデバフを付与しました！");
                }
            }
        }

        public bool OnPlayerTakeDamage()
        {
            if (HasBuff(BuffType.DivineProtection) && isShieldReady)
            {
                isShieldReady = false;
                shieldCooldownTimer = 15f;
                Debug.Log("[聖なる守り] 攻撃を無効化！");
                return true;
            }

            if (HasBuff(BuffType.Vengeance)) AddBuff(BuffType.AttackUp, 0.3f, 5f);
            if (HasBuff(BuffType.FleshCut))
            {
                AddBuff(BuffType.AttackUp, 0.4f, 5f);
                AddBuff(BuffType.CooldownReduction, 0.3f, 5f);
            }

            return false;
        }

        public void OnItemPickedUp()
        {
            if (HasBuff(BuffType.GraveRobber)) AddBuff(BuffType.AttackUp, 0.25f, 6f);
        }

        public void RegisterKill()
        {
            if (!HasBuff(BuffType.PathToAscension) || isAscended) return;

            currentKills++;
            if (currentKills >= 50)
            {
                isAscended = true;
                ClearBuffsOfType(BuffType.MagicSlotReduction);
                if (player != null) player.SetInvincible(true);
                Debug.Log("[神仙へと至る道] 覚醒完了！無敵化しました。");
            }
        }

        private bool IsAllSlotsSame()
        {
            if (player == null || player.Inventory == null || player.Inventory.spellSlots == null) return false;
            var slots = player.Inventory.spellSlots;
            if (slots.Length == 0) return false;

            MagicData first = slots[0];
            for (int i = 1; i < slots.Length; i++)
            {
                if (slots[i] != first) return false;
            }
            return true;
        }

        #endregion
    }
}