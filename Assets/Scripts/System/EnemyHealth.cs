using UnityEngine;
using UnityEngine.Events;

namespace MagicRogue
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("HP設定")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("参照 (自動取得)")]
        [SerializeField] private EnemyController controller;

        [Header("イベント")]
        public UnityEvent<float, float> OnHealthChanged; // (current, max)
        public UnityEvent OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        private bool isDead = false;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<EnemyController>();
        }

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void SetMaxHealth(float maxHp)
        {
            maxHealth = Mathf.Max(1f, maxHp);
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            OnDeath?.Invoke();

            if (controller != null)
            {
                controller.Die();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}