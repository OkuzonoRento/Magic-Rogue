using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    public enum EnemyState
    {
        Patrol,     // 徘徊
        Chase,      // 追跡
        Attack,     // 攻撃
        Retreat     // 退避
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("基本設定")]
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private NavMeshAgent agent;

        [Header("回転設定")]
        [SerializeField] private float rotationSpeed = 10f;     // プレイヤーに向く旋回速度

        [Header("コンボ・連撃設定")]
        [SerializeField] private int minComboCount = 1;         // 1回で繰り出す最小攻撃回数
        [SerializeField] private int maxComboCount = 3;         // 1回で繰り出す最大攻撃回数
        [SerializeField] private float comboInterval = 0.5f;     // 連撃間のインターバル(秒)
        [SerializeField] private float globalAttackCooldown = 2f; // 全体攻撃後の共通クールタイム(秒)

        [Header("各種コンポーネント (自動取得)")]
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private Animator animator;
        [SerializeField] private BuffHandler buffHandler;

        private Transform targetPlayer;
        private EnemyState currentState = EnemyState.Patrol;

        private float nextAllowedAttackTime = 0f;
        private float retreatTimer = 0f;
        private Vector3 patrolDestination;
        private bool hasPatrolDestination = false;
        private bool isDead = false;

        // 連撃制御用変数
        private int remainingComboHits = 0;
        private bool isAttackingAnimation = false;
        private AttackPattern currentSelectedPattern;

        public EnemyData Data => enemyData;

        private void Start()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (buffHandler == null) buffHandler = GetComponent<BuffHandler>();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }

            if (enemyData != null)
            {
                if (enemyHealth != null) enemyHealth.SetMaxHealth(enemyData.maxHp);
                ApplySpeedMultiplier();
            }

            SetState(EnemyState.Patrol);
        }

        private void Update()
        {
            if (isDead) return;

            ApplySpeedMultiplier();

            switch (currentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrolState();
                    break;
                case EnemyState.Chase:
                    UpdateChaseState();
                    break;
                case EnemyState.Attack:
                    UpdateAttackState();
                    break;
                case EnemyState.Retreat:
                    UpdateRetreatState();
                    break;
            }

            if (animator != null && agent != null)
            {
                bool isWalking = agent.velocity.magnitude > 0.1f;
                animator.SetBool("walk", isWalking);
            }
        }

        private void ApplySpeedMultiplier()
        {
            if (agent == null || enemyData == null) return;

            float speedMult = 1f;
            if (buffHandler != null)
            {
                speedMult *= buffHandler.GetMultiplier(BuffType.MoveSpeedUp, BuffType.MoveSpeedDown);
            }
            agent.speed = enemyData.moveSpeed * speedMult;
        }

        private void SetState(EnemyState newState)
        {
            currentState = newState;
            hasPatrolDestination = false;

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.updateRotation = true; // 基本状態では NavMeshAgent に回転させる
            }
        }

        #region State Updates

        private void UpdatePatrolState()
        {
            if (targetPlayer == null || enemyData == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            if (distanceToPlayer <= enemyData.sightRange)
            {
                Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
                dirToPlayer.y = 0f;

                float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

                if (angleToPlayer <= enemyData.sightAngle * 0.5f)
                {
                    SetState(EnemyState.Chase);
                    return;
                }
            }

            if (!hasPatrolDestination || (agent != null && agent.remainingDistance <= 0.5f))
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    patrolDestination = hit.position;
                    hasPatrolDestination = true;
                    if (agent != null && agent.isActiveAndEnabled) agent.SetDestination(patrolDestination);
                }
            }
        }

        private void UpdateChaseState()
        {
            if (targetPlayer == null || enemyData == null)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            if (distanceToPlayer > enemyData.sightRange * 1.5f)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            // クールタイムが明けており、攻撃可能範囲内の技があれば Attack へ移行
            if (Time.time >= nextAllowedAttackTime)
            {
                AttackPattern validPattern = GetRandomAvailableAttackPattern(distanceToPlayer);
                if (validPattern != null)
                {
                    // 連撃回数をランダムで決定
                    remainingComboHits = Random.Range(minComboCount, maxComboCount + 1);
                    currentSelectedPattern = validPattern;
                    SetState(EnemyState.Attack);
                    return;
                }
            }

            // 追尾移動
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPlayer.position);
            }
        }

        private void UpdateAttackState()
        {
            if (targetPlayer == null || enemyData == null)
            {
                EndAttackAndCooldown();
                SetState(EnemyState.Patrol);
                return;
            }

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.updateRotation = false; // 手動で回転補間を行うため自動回転をオフ
            }

            // モーション再生中でなければ、プレイヤーの方向へ滑らかに向きを変える
            if (!isAttackingAnimation)
            {
                RotateTowardsTarget(targetPlayer.position);
            }
            else
            {
                // アニメーション再生中は何もしない（向きを固定）
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

            // 連撃がまだ残っている場合
            if (remainingComboHits > 0)
            {
                // 次の攻撃パターンを選択（届く技があれば）
                currentSelectedPattern = GetRandomAvailableAttackPattern(distanceToPlayer);

                // 範囲外に逃げられた（発動できる技がない）場合は連撃を中断して Chase へ戻る
                if (currentSelectedPattern == null)
                {
                    EndAttackAndCooldown();
                    SetState(EnemyState.Chase);
                    return;
                }

                // 攻撃モーション実行
                StartCoroutine(ExecuteAttackRoutine());
            }
            else
            {
                // 連撃終了時 -> 全体クールタイムを適用して Chase に戻る
                EndAttackAndCooldown();
                SetState(EnemyState.Chase);
            }
        }

        /// <summary>
        /// 指定した目標位置へ滑らかに回転させる
        /// </summary>
        private void RotateTowardsTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        private IEnumerator ExecuteAttackRoutine()
        {
            isAttackingAnimation = true;
            remainingComboHits--;

            TriggerAttackAnimation();

            // アニメーション再生・判定待ち（Animation EventでOnEnemyAttackAnimationが呼ばれる）
            yield return new WaitForSeconds(comboInterval);

            isAttackingAnimation = false;
        }

        private void EndAttackAndCooldown()
        {
            remainingComboHits = 0;
            isAttackingAnimation = false;

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.updateRotation = true;
            }

            // 技ごとの個別のクールタイムではなく、全体クールタイムを適用
            nextAllowedAttackTime = Time.time + globalAttackCooldown;
        }

        private void UpdateRetreatState()
        {
            if (targetPlayer == null)
            {
                SetState(EnemyState.Patrol);
                return;
            }

            retreatTimer -= Time.deltaTime;
            if (retreatTimer <= 0f)
            {
                SetState(EnemyState.Chase);
                return;
            }

            Vector3 retreatDir = (transform.position - targetPlayer.position).normalized;
            Vector3 targetPos = transform.position + retreatDir * 5f;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
        }

        #endregion

        /// <summary>
        /// 現在の距離・範囲にヒットする攻撃パターン一覧の中からランダムで1つ取得する
        /// </summary>
        private AttackPattern GetRandomAvailableAttackPattern(float distanceToPlayer)
        {
            if (enemyData == null || enemyData.attackPatterns == null || enemyData.attackPatterns.Count == 0) return null;

            List<AttackPattern> availablePatterns = new List<AttackPattern>();

            foreach (var pattern in enemyData.attackPatterns)
            {
                if (pattern.attackType == AttackType.Circle)
                {
                    if (AttackChecker.IsInCircleRange(transform, targetPlayer.position, pattern))
                    {
                        availablePatterns.Add(pattern);
                    }
                }
                else if (pattern.attackType == AttackType.RangedTarget)
                {
                    if (distanceToPlayer >= pattern.minAttackRange && distanceToPlayer <= pattern.maxAttackRange)
                    {
                        availablePatterns.Add(pattern);
                    }
                }
            }

            if (availablePatterns.Count > 0)
            {
                int randomIndex = Random.Range(0, availablePatterns.Count);
                return availablePatterns[randomIndex];
            }

            return null;
        }

        private void TriggerAttackAnimation()
        {
            if (animator != null && currentSelectedPattern != null && !string.IsNullOrEmpty(currentSelectedPattern.animationTriggerName))
            {
                animator.SetTrigger(currentSelectedPattern.animationTriggerName);
            }
            else if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }

        /// <summary>
        /// アニメーションイベントから呼び出される攻撃判定実行メソッド
        /// (Animation Event name: OnEnemyAttackAnimation)
        /// </summary>
        public void OnEnemyAttackAnimation()
        {
            if (isDead || targetPlayer == null || currentSelectedPattern == null) return;

            bool isStillInRange = AttackChecker.IsTargetInAttackRange(transform, targetPlayer, currentSelectedPattern, targetPlayer.position);

            if (isStillInRange)
            {
                float atkMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.AttackUp, BuffType.AttackDown) : 1f;
                float finalDamage = currentSelectedPattern.damage * atkMult;

                if (targetPlayer.TryGetComponent<PlayerController>(out var playerController))
                {
                    playerController.TakeDamage(finalDamage);
                }
            }
        }

        public void TakeDamage(float damageAmount)
        {
            if (isDead) return;

            float damageMult = buffHandler != null ? buffHandler.GetMultiplier(BuffType.DamageReceivedUp) : 1f;
            float finalDamage = damageAmount * damageMult;

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
            }
            else
            {
                Die();
            }

            if (!isDead)
            {
                if (targetPlayer == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) targetPlayer = playerObj.transform;
                }

                if (targetPlayer != null && currentState != EnemyState.Attack)
                {
                    SetState(EnemyState.Chase);
                }
            }
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            StopAllCoroutines();
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;

            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.OnEnemyKilled();
            }

            if (targetPlayer != null && targetPlayer.TryGetComponent<BuffHandler>(out var playerBuffs))
            {
                playerBuffs.RegisterKill();
            }

            DropItems();
            Destroy(gameObject);
        }

        private void DropItems()
        {
            if (enemyData == null || enemyData.dropList == null) return;

            foreach (var drop in enemyData.dropList)
            {
                if (drop.item != null && Random.value <= drop.dropChance)
                {
                    if (drop.item.customWorldModelPrefab != null)
                    {
                        Instantiate(drop.item.customWorldModelPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    }
                }
            }
        }
    }
}