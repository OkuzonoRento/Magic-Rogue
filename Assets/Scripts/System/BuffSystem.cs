using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public enum BuffType
    {
        AttackUp,     // 攻撃力バフ (%)
        DefenseUp,    // 防御力バフ (%)
        SpeedUp,      // 移動速度バフ (%)
        Slow,         // 移動速度デバフ (%)
        Poison        // 毒（持続ダメージ）
    }

    [Serializable]
    public class BuffInstance
    {
        public BuffType type;
        public float value;       // 変化量 (例: 0.2 = +20%)
        public float duration;    // 残り時間（秒）
        public float tickTimer;   // 毒などの定期処理用タイマー

        public BuffInstance(BuffType type, float value, float duration)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.tickTimer = 0f;
        }
    }

    public class BuffHandler : MonoBehaviour
    {
        private readonly List<BuffInstance> activeBuffs = new List<BuffInstance>();

        public event Action<BuffType, float> OnPoisonTick;

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.duration -= deltaTime;

                // 毒ダメージ等の持続系処理
                if (buff.type == BuffType.Poison)
                {
                    buff.tickTimer += deltaTime;
                    if (buff.tickTimer >= 1.0f)
                    {
                        buff.tickTimer -= 1.0f;
                        OnPoisonTick?.Invoke(buff.type, buff.value);
                    }
                }

                if (buff.duration <= 0f)
                {
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        public void AddBuff(BuffType type, float value, float duration)
        {
            activeBuffs.Add(new BuffInstance(type, value, duration));
        }

        /// <summary>
        /// 指定したバフタイプの合計倍率を取得 (例: +20% と +10% なら 1.3)
        /// </summary>
        public float GetMultiplier(BuffType positiveBuff, BuffType negativeBuff = (BuffType)(-1))
        {
            float multiplier = 1.0f;

            foreach (var buff in activeBuffs)
            {
                if (buff.type == positiveBuff)
                {
                    multiplier += buff.value;
                }
                else if (negativeBuff >= 0 && buff.type == negativeBuff)
                {
                    multiplier -= buff.value;
                }
            }

            return Mathf.Max(0.1f, multiplier); // 最低10%保証
        }
    }
}