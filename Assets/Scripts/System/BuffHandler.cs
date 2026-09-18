using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public class BuffHandler : MonoBehaviour
    {
        private readonly List<BuffInstance> activeBuffs = new List<BuffInstance>();

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.duration -= deltaTime;

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

            return Mathf.Max(0.1f, multiplier);
        }

        public float GetTotalValue(BuffType type)
        {
            float total = 0f;
            foreach (var buff in activeBuffs)
            {
                if (buff.type == type)
                {
                    total += buff.value;
                }
            }
            return total;
        }

        public bool CheckIsFailed()
        {
            float failChance = GetTotalValue(BuffType.ChanceToFail);
            if (failChance <= 0f) return false;

            return UnityEngine.Random.value < failChance;
        }

        public bool HasBuff(BuffType type)
        {
            return activeBuffs.Exists(b => b.type == type);
        }

        public void ClearBuffsOfType(BuffType type)
        {
            activeBuffs.RemoveAll(b => b.type == type);
        }
    }
}