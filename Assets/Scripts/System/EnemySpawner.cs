using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("配置済みマップリスト")]
        [SerializeField] private List<MapController> allMaps = new List<MapController>();

        [Header("参照")]
        [SerializeField] private Transform playerTransform;

        [Header("スポーン距離設定")]
        [Tooltip("プレイヤーからの最小距離（視界/サーチ圏外判定）")]
        [SerializeField] private float minSpawnDistance = 15f;

        [Tooltip("プレイヤーからの最大距離")]
        [SerializeField] private float maxSpawnDistance = 35f;

        [Tooltip("NavMesh上の位置検索の許容誤差")]
        [SerializeField] private float navMeshSampleDistance = 5f;

        private MapController activeMap;
        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private Coroutine spawnCoroutine;

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            // 初期マップの選択（必要に応じて外部から呼び出してください）
            SelectMap(0);
        }

        public void SelectMap(int mapIndex)
        {
            if (mapIndex < 0 || mapIndex >= allMaps.Count) return;

            for (int i = 0; i < allMaps.Count; i++)
            {
                if (i == mapIndex)
                {
                    activeMap = allMaps[i];
                    activeMap.ActivateMap();
                }
                else
                {
                    allMaps[i].DeactivateMap();
                }
            }

            // プレイヤーをマップ指定のスポーン位置へワープさせる
            if (activeMap != null && activeMap.PlayerSpawnPoint != null && playerTransform != null)
            {
                // CharacterController が付いている場合は移動前に一時無効化する
                if (playerTransform.TryGetComponent<CharacterController>(out var controller))
                {
                    controller.enabled = false;
                    playerTransform.position = activeMap.PlayerSpawnPoint.position;
                    playerTransform.rotation = activeMap.PlayerSpawnPoint.rotation;
                    controller.enabled = true;
                }
                else
                {
                    playerTransform.position = activeMap.PlayerSpawnPoint.position;
                    playerTransform.rotation = activeMap.PlayerSpawnPoint.rotation;
                }
            }

            // 敵のスポーン処理を開始
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            if (activeMap != null && activeMap.MapData != null)
            {
                spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }

        private IEnumerator SpawnRoutine()
        {
            var mapData = activeMap.MapData;

            while (true)
            {
                yield return new WaitForSeconds(mapData.spawnInterval);

                activeEnemies.RemoveAll(enemy => enemy == null);

                if (activeEnemies.Count >= mapData.maxEnemyCount)
                {
                    continue;
                }

                // マップのNavMesh上から自動計算でスポーン位置を取得
                if (TryGetAutoNavMeshSpawnPosition(out Vector3 spawnPosition))
                {
                    GameObject selectedPrefab = SelectRandomEnemyPrefab(mapData);
                    if (selectedPrefab != null)
                    {
                        GameObject enemy = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
                        activeEnemies.Add(enemy);
                    }
                }
            }
        }

        // NavMeshから自動で有効なスポーン位置を取得（座標登録不要）
        private bool TryGetAutoNavMeshSpawnPosition(out Vector3 result)
        {
            result = Vector3.zero;
            if (playerTransform == null || activeMap == null) return false;

            // プレイヤーの周りかつ視界外（minSpawnDistance 〜 maxSpawnDistance）からランダムサンプリング
            for (int i = 0; i < 15; i++)
            {
                // プレイヤーを中心としたドーナツ状の範囲からランダムに方向と距離を選出
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 candidatePos = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // 選んだランダム座標の足元・近傍に NavMesh があるか確認して吸着
                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    // 実際にプレイヤーとの直線距離が視界外か確認
                    if (Vector3.Distance(playerTransform.position, hit.position) >= minSpawnDistance)
                    {
                        result = hit.position;
                        return true;
                    }
                }
            }

            return false;
        }

        private GameObject SelectRandomEnemyPrefab(MapData mapData)
        {
            if (mapData.spawnableEnemies == null || mapData.spawnableEnemies.Count == 0) return null;

            int totalWeight = 0;
            foreach (var config in mapData.spawnableEnemies)
            {
                totalWeight += config.spawnWeight;
            }

            int randomValue = Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var config in mapData.spawnableEnemies)
            {
                currentSum += config.spawnWeight;
                if (randomValue < currentSum)
                {
                    return config.enemyPrefab;
                }
            }

            return mapData.spawnableEnemies[0].enemyPrefab;
        }
    }
}