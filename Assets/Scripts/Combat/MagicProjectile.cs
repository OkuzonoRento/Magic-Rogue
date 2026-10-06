using UnityEngine;

namespace MagicRogue
{
    public class MagicProjectile : MonoBehaviour
    {
        [Header("弾パラメータ（Setup時にMagicDataから自動設定）")]
        private MagicData magicData;
        private Vector3 moveDirection;
        private float attackMultiplier = 1f;
        private BuffHandler ownerPlayerBuffHandler;

        /// <summary>
        /// 弾の初期化メソッド
        /// </summary>
        public void Setup(MagicData data, Vector3 direction, float atkMultiplier, BuffHandler ownerBuffs = null)
        {
            magicData = data;
            moveDirection = direction.normalized;
            attackMultiplier = atkMultiplier;

            if (ownerBuffs != null)
            {
                ownerPlayerBuffHandler = ownerBuffs;
            }
            else
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    ownerPlayerBuffHandler = player.GetComponent<BuffHandler>();
                }
            }

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            // ★ MagicData の duration (生存時間) を参照して自動消滅
            float lifetime = (magicData != null && magicData.duration > 0f) ? magicData.duration : 3.0f;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (magicData == null) return;

            // 直進移動
            transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (other.TryGetComponent<EnemyController>(out var enemy))
                {
                    float finalDamage = (magicData != null ? magicData.damage : 10f) * attackMultiplier;
                    enemy.TakeDamage(finalDamage);

                    // 敵の GameObject を引数として渡してバフ・デバフ（呪いの杖等）を伝播
                    if (ownerPlayerBuffHandler != null)
                    {
                        ownerPlayerBuffHandler.OnMagicHitEnemy(other.gameObject);
                    }
                }

                if (magicData != null && magicData.hitEffectPrefab != null)
                {
                    Instantiate(magicData.hitEffectPrefab, transform.position, transform.rotation);
                }

                Destroy(gameObject);
            }
            else if (other.CompareTag("Environment") || other.CompareTag("Wall"))
            {
                if (magicData != null && magicData.hitEffectPrefab != null)
                {
                    Instantiate(magicData.hitEffectPrefab, transform.position, transform.rotation);
                }

                Destroy(gameObject);
            }
        }
    }
}