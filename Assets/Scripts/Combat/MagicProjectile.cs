using System.Collections;
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

        // 内部状態用変数
        private Transform targetEnemyTransform;
        private Transform playerTransform;
        private float elapsedTime = 0f;
        private bool isReturning = false;
        private int currentSplitCount = 0;

        // 散弾（Split）用：直前に当たった敵（自身を生成した元の敵）を記憶して除外する
        private GameObject hitTargetToIgnore = null;

        /// <summary>
        /// 弾の初期化メソッド
        /// </summary>
        public void Setup(MagicData data, Vector3 direction, float atkMultiplier, BuffHandler ownerBuffs = null, int splitCount = 0, GameObject ignoreEnemy = null)
        {
            magicData = data;
            moveDirection = direction.normalized;
            attackMultiplier = atkMultiplier;
            currentSplitCount = splitCount;
            hitTargetToIgnore = ignoreEnemy;

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

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            // 生存時間による自動消滅
            float lifetime = (magicData != null && magicData.duration > 0f) ? magicData.duration : 3.0f;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (magicData == null) return;

            elapsedTime += Time.deltaTime;

            switch (magicData.movementType)
            {
                case MovementType.Homing:
                    MoveHoming();
                    break;

                case MovementType.Boomerang:
                    MoveBoomerang();
                    break;

                case MovementType.Straight:
                case MovementType.Spread:
                case MovementType.Split:
                default:
                    // 通常直進移動
                    transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
                    break;
            }
        }

        #region 移動制御ロジック

        /// <summary>
        /// Homing: 生成から一定時間Wait後に追尾を開始し、一定時間経過すると直進に戻る
        /// </summary>
        private void MoveHoming()
        {
            // 例: 生成から0.2秒間は直進（Wait時間）、0.2s〜2.0sの間だけ追尾、2.0s以降は直進
            float homingDelay = 0.2f;
            float homingDuration = 2.0f;

            if (elapsedTime >= homingDelay && elapsedTime < (homingDelay + homingDuration))
            {
                if (targetEnemyTransform == null || !targetEnemyTransform.gameObject.activeInHierarchy)
                {
                    FindNearestEnemy();
                }

                if (targetEnemyTransform != null)
                {
                    Vector3 targetDir = (targetEnemyTransform.position - transform.position).normalized;
                    // 滑らかにターゲット方向へ補間
                    moveDirection = Vector3.RotateTowards(moveDirection, targetDir, 8f * Time.deltaTime, 0f).normalized;
                    if (moveDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(moveDirection);
                    }
                }
            }

            transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Boomerang: 一定時間後に方向転換を開始し、プレイヤーの位置へ滑らかに旋回・戻り到達時に消滅する
        /// </summary>
        private void MoveBoomerang()
        {
            if (!isReturning && elapsedTime >= magicData.returnTime)
            {
                isReturning = true;
            }

            if (isReturning && playerTransform != null)
            {
                Vector3 returnDir = (playerTransform.position - transform.position).normalized;

                // カクッとではなく、RotateTowardsで滑らかにプレイヤー方向へ向けて旋回
                moveDirection = Vector3.RotateTowards(moveDirection, returnDir, 6f * Time.deltaTime, 0f).normalized;
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }

                float currentSpeed = magicData.projectileSpeed * magicData.returnSpeedMultiplier;
                transform.position += moveDirection * currentSpeed * Time.deltaTime;

                // 自身（プレイヤー）との距離が近くに戻ってきたら削除
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer < 1.2f)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
            }
        }

        private void FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float minDistance = float.MaxValue;
            Transform nearest = null;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                // 分裂弾などで特定の除外対象（当たった直後の敵）がいる場合はターゲット検索から除外
                if (hitTargetToIgnore != null && enemy == hitTargetToIgnore) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy.transform;
                }
            }

            targetEnemyTransform = nearest;
        }

        #endregion

        #region 衝突・ヒット時ロジック

        private void OnTriggerEnter(Collider other)
        {
            if (magicData == null) return;

            if (other.CompareTag("Enemy"))
            {
                // 分裂直後に当たったばかりの元の敵には当たらないように無視する
                if (hitTargetToIgnore != null && other.gameObject == hitTargetToIgnore)
                {
                    return;
                }

                if (other.TryGetComponent<EnemyController>(out var enemy))
                {
                    float finalDamage = magicData.damage * attackMultiplier;
                    enemy.TakeDamage(finalDamage);

                    if (ownerPlayerBuffHandler != null)
                    {
                        ownerPlayerBuffHandler.OnMagicHitEnemy(other.gameObject);
                    }
                }

                if (magicData.hitEffectPrefab != null)
                {
                    Instantiate(magicData.hitEffectPrefab, transform.position, transform.rotation);
                }

                // 命中時分裂 (Split) の判定
                if (magicData.movementType == MovementType.Split && currentSplitCount < magicData.maxSplitCount)
                {
                    TriggerSplitProjectiles(other.gameObject);
                }

                // 敵貫通 (piercesEnemy) の判定
                if (!magicData.piercesEnemy)
                {
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag("Environment") || other.CompareTag("Wall"))
            {
                if (magicData.hitEffectPrefab != null)
                {
                    Instantiate(magicData.hitEffectPrefab, transform.position, transform.rotation);
                }

                // 壁貫通 (piercesWall) の判定
                if (!magicData.piercesWall)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// 命中時に周囲へ小弾を放射（Split挙動）
        /// 当たった敵(hitEnemy)を除外指定して小弾を生成し、2回目以降の分裂(連鎖)では元のターゲットにも当たるようにする
        /// </summary>
        private void TriggerSplitProjectiles(GameObject hitEnemy)
        {
            int count = magicData.splitSubProjectiles > 0 ? magicData.splitSubProjectiles : 4;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                Vector3 subDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                GameObject subObj = Instantiate(
                    magicData.projectilePrefab != null ? magicData.projectilePrefab : gameObject,
                    transform.position,
                    Quaternion.LookRotation(subDir)
                );

                if (subObj.TryGetComponent<MagicProjectile>(out var subProj))
                {
                    // 当たった敵(hitEnemy)を除外対象として指定。
                    // 次にさらに別の敵へ当たって2回目の分裂が起きた場合は、新しいhitEnemyが渡されるため「最初のターゲット」にも再び当たるようになります。
                    subProj.Setup(magicData, subDir, attackMultiplier * 0.5f, ownerPlayerBuffHandler, currentSplitCount + 1, hitEnemy);
                }
            }
        }

        #endregion
    }
}