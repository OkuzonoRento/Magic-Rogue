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

        public void Setup(MagicData data, Vector3 direction, float damageMultiplier = 1f, int currentSplits = -1, GameObject ignoredEnemy = null)
        {
            magicData = data;
            moveDirection = new Vector3(direction.x, 0f, direction.z).normalized; // Y軸は固定
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
            if (targetEnemy != null)
            {
                Vector3 targetDir = (targetEnemy.position - transform.position);
                targetDir.y = 0f; // 高さは固定
                targetDir.Normalize();

                moveDirection = Vector3.Slerp(moveDirection, targetDir, Time.deltaTime * 6f);
            }
            transform.position += moveDirection * magicData.projectileSpeed * Time.deltaTime;
        }

        private void MoveBoomerang()
        {
            if (!isReturning && Time.time - spawnTime >= magicData.returnTime)
            {
                isReturning = true;
            }

            if (isReturning && ownerPlayer != null)
            {
                // 水平面（XZ）上での戻り方向を計算
                Vector3 targetPos = ownerPlayer.position;
                targetPos.y = transform.position.y; // Y座標（高さ）を維持

                Vector3 returnDir = (targetPos - transform.position).normalized;
                moveDirection = Vector3.Slerp(moveDirection, returnDir, Time.deltaTime * 8f);
                transform.position += moveDirection * (magicData.projectileSpeed * magicData.returnSpeedMultiplier) * Time.deltaTime;

                // プレイヤー手元（XZ平面の距離）に戻ったら消滅
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
            // プレイヤーに追従してビームの位置と向きを固定
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
            // 1. プレイヤー、他の弾、およびタグが設定されていないオブジェクト（床や発射地点の背景）との接触を無視
            if (other.CompareTag("Player") || other.CompareTag("Untagged") || other.GetComponent<MagicProjectile>() != null)
            {
                return;
            }

            // 2. 敵（Enemy）へのヒット処理
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
            // 3. 明確に「Wall」や「Environment」タグが付いた障害物に当たった時のみ消滅させる
            else if (other.CompareTag("Wall") && !magicData.piercesWall && magicData.movementType != MovementType.Laser)
            {
                PlayHitVFX();
                Destroy(gameObject);
            }
        }

        private void ApplyDamage(GameObject enemyObj)
        {
            Debug.Log($"[Hit] {enemyObj.name} に {finalDamage} ダメージ！");
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