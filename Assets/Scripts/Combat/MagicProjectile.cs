using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    [RequireComponent(typeof(Collider))]
    public class MagicProjectile : MonoBehaviour
    {
        private MagicData magicData;
        private Vector3 moveDirection;
        private float finalDamage;
        private Transform targetEnemy;
        private Transform ownerPlayer;

        private int remainingSplits;
        private GameObject lastHitEnemy;
        private float spawnTime;
        private bool isReturning = false;
        private float laserTimer = 0f;

        [Header("Homing Settings")]
        [SerializeField] private float homingDelay = 0.25f;

        public void Setup(MagicData data, Vector3 direction, float damageMultiplier = 1f, int currentSplits = -1, GameObject ignoredEnemy = null)
        {
            magicData = data;
            moveDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            finalDamage = data.damage * damageMultiplier;
            spawnTime = Time.time;
            lastHitEnemy = ignoredEnemy;

            remainingSplits = (currentSplits == -1) ? data.maxSplitCount : currentSplits;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) ownerPlayer = playerObj.transform;

            if (magicData.movementType == MovementType.Homing)
            {
                targetEnemy = FindNearestEnemy();
            }

            Destroy(gameObject, magicData.duration);
        }

        private void Update()
        {
            if (magicData == null) return;

            switch (magicData.movementType)
            {
                case MovementType.Straight:
                case MovementType.Spread:
                case MovementType.Split:
                    MoveStraight();
                    break;

                case MovementType.Homing:
                    MoveHoming();
                    break;

                case MovementType.Laser:
                    UpdateLaser();
                    break;

                case MovementType.Boomerang:
                    MoveBoomerang();
                    break;
            }
        }

        private void MoveStraight()
        {
            transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
        }

        private void MoveHoming()
        {
            if (Time.time - spawnTime >= homingDelay)
            {
                if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
                {
                    targetEnemy = FindNearestEnemy();
                }

                if (targetEnemy != null)
                {
                    Vector3 targetDir = (targetEnemy.position - transform.position);
                    targetDir.y = 0f;
                    float distanceToTarget = targetDir.magnitude;
                    targetDir.Normalize();

                    float currentTurnSpeed = (distanceToTarget < 3.0f) ? 25f : 12f;

                    moveDirection = Vector3.Slerp(moveDirection, targetDir, Time.deltaTime * currentTurnSpeed);
                }
            }

            transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;

            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }

        private void MoveBoomerang()
        {
            if (!isReturning && Time.time - spawnTime >= magicData.returnTime)
            {
                isReturning = true;
            }

            if (isReturning && ownerPlayer != null)
            {
                Vector3 targetPos = ownerPlayer.position;
                targetPos.y = transform.position.y;

                Vector3 returnDir = (targetPos - transform.position).normalized;
                moveDirection = Vector3.Slerp(moveDirection, returnDir, Time.deltaTime * 8f);
                transform.position += moveDirection * (magicData.projectileSpeed * magicData.returnSpeedMultiplier) * Time.deltaTime;

                if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                     new Vector3(ownerPlayer.position.x, 0, ownerPlayer.position.z)) < 0.8f)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                MoveStraight();
            }
        }

        private void UpdateLaser()
        {
            if (ownerPlayer != null)
            {
                transform.position = ownerPlayer.position + Vector3.up * 1f;
                transform.rotation = ownerPlayer.rotation;
            }
            laserTimer += Time.deltaTime;
        }

        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject nearest = null;
            float minDistance = float.MaxValue;

            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy;
                }
            }
            return nearest != null ? nearest.transform : null;
        }

        private void OnTriggerStay(Collider other)
        {
            if (magicData.movementType == MovementType.Laser && other.CompareTag("Enemy"))
            {
                if (laserTimer >= magicData.laserDamageInterval)
                {
                    laserTimer = 0f;
                    ApplyDamage(other.gameObject);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Untagged") || other.GetComponent<MagicProjectile>() != null)
            {
                return;
            }

            if (other.CompareTag("Enemy"))
            {
                if (other.gameObject == lastHitEnemy) return;

                ApplyDamage(other.gameObject);

                if (magicData.movementType == MovementType.Split && remainingSplits > 0)
                {
                    SplitIntoFour(other.gameObject);
                    Destroy(gameObject);
                    return;
                }

                if (!magicData.piercesEnemy && magicData.movementType != MovementType.Laser)
                {
                    PlayHitVFX();
                    Destroy(gameObject);
                }
            }
            else if (other.CompareTag("Wall") && !magicData.piercesWall && magicData.movementType != MovementType.Laser)
            {
                PlayHitVFX();
                Destroy(gameObject);
            }
        }

        private void ApplyDamage(GameObject enemyObj)
        {
            Debug.Log($"[Hit] {enemyObj.name} に {finalDamage} ダメージ！");

            if (enemyObj.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(finalDamage);
            }

            PlayHitVFX();
        }

        private void SplitIntoFour(GameObject hitEnemy)
        {
            float[] angles = new float[] { 45f, 135f, 225f, 315f };

            foreach (float angle in angles)
            {
                Quaternion rot = Quaternion.Euler(0f, angle, 0f);
                Vector3 splitDir = rot * moveDirection;

                GameObject subObj = Instantiate(gameObject, transform.position, Quaternion.LookRotation(splitDir));
                if (subObj.TryGetComponent<MagicProjectile>(out var subProj))
                {
                    subProj.Setup(magicData, splitDir, 1f, remainingSplits - 1, hitEnemy);
                }
            }
        }

        private void PlayHitVFX()
        {
            if (magicData.hitEffectPrefab != null) Instantiate(magicData.hitEffectPrefab, transform.position, Quaternion.identity);
            if (magicData.hitSound != null) AudioSource.PlayClipAtPoint(magicData.hitSound, transform.position);
        }
    }
}