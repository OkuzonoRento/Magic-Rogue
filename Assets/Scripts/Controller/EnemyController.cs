using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    public enum EnemyState
    {
        IdleOrWander,   // 待機・ランダムウォーク
        Chasing,        // 追跡
        Attacking,      // 攻撃中
        Kiting,         // 後ずさり（攻撃のクールタイム中）
        GivingUp        // 諦めて初期位置へ帰還中
    }

    [RequireComponent(typeof(BuffHandler))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("敵データ")]
        public EnemyData enemyData;

        [Header("ターゲット情報")]
        public PlayerController targetPlayer;

        [Header("感知設定")]
        public float immediateSenseRadius = 2.5f;

        [Header("ランダムウォーク設定")]
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderInterval = 3.5f;

        [Header("引き行動（後ずさり）設定")]
        [SerializeField] private float retreatDistance = 4.5f;

        [Header("諦め（ギブアップ）設定")]
        [SerializeField] private float giveUpTime = 7f;
        [SerializeField] private float resetCooldown = 5f;

        [Header("ドロップ設定")]
        [SerializeField] private GameObject itemPickupBasePrefab;

        public EnemyState CurrentState { get; private set; } = EnemyState.IdleOrWander;
        public bool IsAlerted { get; private set; } = false;
        public NavMeshAgent Agent => agent;

        private BuffHandler buffHandler;
        private NavMeshAgent agent;
        private float currentHp;
        private bool isDead = false;

        private float wanderTimer;
        private float attackTimer;
        private bool isAttackRoutineRunning = false;
        private bool isGivingUp = false;
        private Vector3 spawnPosition;

        private void Awake()
        {
            buffHandler = GetComponent<BuffHandler>();
            agent = GetComponent<NavMeshAgent>();

            if (targetPlayer == null)
            {
                targetPlayer = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Start()
        {
            spawnPosition = transform.position;

            if (GameSceneManager.Instance != null && buffHandler != null)
            {
                GameSceneManager.Instance.ApplyAllBuffsToEnemy(buffHandler);
            }

            float statMult = (buffHandler != null) ? buffHandler.GetMultiplier(BuffType.EnemyStatUp, BuffType.EnemyStatDown) : 1.0f;

            if (enemyData != null)
            {
                currentHp = enemyData.maxHp * statMult;
                agent.speed = enemyData.moveSpeed * statMult;

                agent.stoppingDistance = GetCenterAttackRange();
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority = Random.Range(30, 60);
            }
            else
            {
                currentHp = 50f * statMult;
            }

            wanderTimer = wanderInterval;
        }

        private void Update()
        {
            if (isDead || targetPlayer == null || enemyData == null) return;

            if (isGivingUp)
            {
                HandleGivingUp();
                return;
            }

            CheckSenses();

            switch (CurrentState)
            {
                case EnemyState.IdleOrWander:
                    HandleWander();
                    break;

                case EnemyState.Chasing:
                    HandleChasing();
                    break;

                case EnemyState.Attacking:
                    if (agent.hasPath) agent.ResetPath();
                    break;

                case EnemyState.Kiting:
                    HandleKiting();
                    break;
            }
        }

        private void CheckSenses()
        {
            if (isGivingUp) return;

            Vector3 directionToPlayer = targetPlayer.transform.position - transform.position;
            directionToPlayer.y = 0f;
            float distanceToPlayer = directionToPlayer.magnitude;

            if (IsAlerted || distanceToPlayer <= immediateSenseRadius)
            {
                SetAlerted();
                return;
            }

            if (distanceToPlayer <= enemyData.sightRange)
            {
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
                if (angleToPlayer <= enemyData.sightAngle * 0.5f)
                {
                    SetAlerted();
                }
            }
        }

        public void SetAlerted()
        {
            if (isGivingUp) return;

            IsAlerted = true;
            if (CurrentState == EnemyState.IdleOrWander)
            {
                CurrentState = EnemyState.Chasing;
                attackTimer = 0f;
            }
        }

        private void HandleWander()
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }

        private void HandleChasing()
        {
            Vector3 directionToPlayer = targetPlayer.transform.position - transform.position;
            directionToPlayer.y = 0f;
            float distanceToPlayer = directionToPlayer.magnitude;

            agent.SetDestination(targetPlayer.transform.position);

            if (distanceToPlayer <= agent.stoppingDistance && !isAttackRoutineRunning)
            {
                attackTimer = 0f;
                StartCoroutine(AttackRoutine());
            }
            else
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= giveUpTime)
                {
                    StartGiveUp();
                }
            }
        }

        private IEnumerator AttackRoutine()
        {
            isAttackRoutineRunning = true;
            CurrentState = EnemyState.Attacking;

            int patternIndex = Random.Range(0, enemyData.attackPatterns.Count);
            var selectedPattern = enemyData.attackPatterns[patternIndex];

            Vector3 lookDir = (targetPlayer.transform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir.normalized);
            }

            OnAttackHit(patternIndex);

            yield return new WaitForSeconds(0.5f);

            CurrentState = EnemyState.Kiting;

            float cooldown = selectedPattern.attackCooldown > 0f ? selectedPattern.attackCooldown : 2f;
            yield return new WaitForSeconds(cooldown);

            CurrentState = EnemyState.Chasing;
            isAttackRoutineRunning = false;
        }

        private void HandleKiting()
        {
            Vector3 dirFromPlayer = (transform.position - targetPlayer.transform.position).normalized;
            dirFromPlayer.y = 0f;

            Vector3 targetPos = targetPlayer.transform.position + dirFromPlayer * retreatDistance;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            Vector3 lookDir = (targetPlayer.transform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 6f);
            }
        }

        private void StartGiveUp()
        {
            isGivingUp = true;
            IsAlerted = false;
            CurrentState = EnemyState.GivingUp;
            agent.SetDestination(spawnPosition);
        }

        private void HandleGivingUp()
        {
            agent.SetDestination(spawnPosition);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StartCoroutine(ResetRoutine());
            }
        }

        private IEnumerator ResetRoutine()
        {
            CurrentState = EnemyState.IdleOrWander;
            yield return new WaitForSeconds(resetCooldown);

            isGivingUp = false;
            attackTimer = 0f;
        }

        private float GetCenterAttackRange()
        {
            if (enemyData == null || enemyData.attackPatterns == null || enemyData.attackPatterns.Count == 0)
                return 1.5f;

            float minCenterRange = float.MaxValue;
            foreach (var pattern in enemyData.attackPatterns)
            {
                float centerRange = (pattern.minAttackRange + pattern.maxAttackRange) * 0.5f;
                if (centerRange < minCenterRange) minCenterRange = centerRange;
            }
            return minCenterRange;
        }

        public void TakeDamage(float damageAmount)
        {
            if (isDead) return;

            currentHp -= damageAmount;

            if (!isGivingUp)
            {
                SetAlerted();
            }

            if (currentHp <= 0f) Die();
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            StopAllCoroutines();
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;

            // 撃破キルカウント処理（神仙へと至る道）
            if (targetPlayer != null && targetPlayer.TryGetComponent<BuffHandler>(out var playerBuffs))
            {
                playerBuffs.RegisterKill();
            }

            DropItems();
            Destroy(gameObject);
        }

        private void DropItems()
        {
            if (enemyData == null || enemyData.dropList == null || itemPickupBasePrefab == null) return;

            foreach (var dropInfo in enemyData.dropList)
            {
                if (dropInfo.item == null) continue;

                float dropMult = 1f;
                if (targetPlayer != null && targetPlayer.TryGetComponent<BuffHandler>(out var playerBuffs))
                {
                    dropMult = playerBuffs.GetMultiplier(BuffType.DropRateUp);
                }

                if (Random.value <= dropInfo.dropChance * dropMult)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                    GameObject dropObj = Instantiate(itemPickupBasePrefab, spawnPos, Quaternion.identity);

                    if (dropObj.TryGetComponent<ItemPickupSystem>(out var pickup))
                    {
                        pickup.Setup(dropInfo.item, 1, triggerPop: true);
                    }
                }
            }
        }

        public void OnAttackHit(int patternIndex)
        {
            if (enemyData == null || enemyData.attackPatterns == null) return;
            if (patternIndex < 0 || patternIndex >= enemyData.attackPatterns.Count) return;
            if (targetPlayer == null) return;

            AttackPattern pattern = enemyData.attackPatterns[patternIndex];
            bool isHit = AttackChecker.IsTargetInAttackRange(transform, targetPlayer.transform, pattern);

            if (isHit)
            {
                if (buffHandler == null) buffHandler = GetComponent<BuffHandler>();

                float selfAtkMult = (buffHandler != null) ? buffHandler.GetMultiplier(BuffType.AttackUp, BuffType.AttackDown) : 1.0f;
                float statMult = (buffHandler != null) ? buffHandler.GetMultiplier(BuffType.EnemyStatUp, BuffType.EnemyStatDown) : 1.0f;

                float finalDamage = pattern.damage * selfAtkMult * statMult;
                targetPlayer.TakeDamage(finalDamage);
            }
        }
    }
}