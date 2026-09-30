using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("配置済みマップリスト")]
        [SerializeField] private List<MapController> allMaps = new List<MapController>();

        [Header("参照")]
        [SerializeField] private Transform playerTransform;

        [Header("スポーン距離設定")]
        [Tooltip("プレイヤーからの最小距離")]
        [SerializeField] private float minSpawnDistance = 15f;

        [Tooltip("プレイヤーからの最大距離")]
        [SerializeField] private float maxSpawnDistance = 35f;

        [Tooltip("NavMesh上の位置検索の許容誤差")]
        [SerializeField] private float navMeshSampleDistance = 5f;

        private MapController activeMap;
        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private Coroutine spawnCoroutine;

        private int currentKillCount = 0;
        private bool isPortalSpawned = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            // GameSceneManager の選択マップ情報に合わせて自動初期化
            InitializeSelectedMap();
        }

        private void InitializeSelectedMap()
        {
            string selectedName = GameSceneManager.Instance != null
                ? GameSceneManager.Instance.CurrentSaveData.selectedMapName
                : string.Empty;

            int targetIndex = 0;

            if (!string.IsNullOrEmpty(selectedName))
            {
                int index = allMaps.FindIndex(m => m.MapData != null && m.MapData.mapName == selectedName);
                if (index >= 0) targetIndex = index;
            }

            SelectMap(targetIndex);
        }

        public void SelectMap(int mapIndex)
        {
            if (mapIndex < 0 || mapIndex >= allMaps.Count) return;

            // 選択されたマップのみをアクティブ化し、それ以外を非アクティブ化
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

            // 1. プレイヤーを選択されたマップのスポーンポイントへ配置
            if (activeMap != null && activeMap.PlayerSpawnPoint != null && playerTransform != null)
            {
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

            // 2. 撃破カウントなどの初期化
            currentKillCount = 0;
            isPortalSpawned = false;

            // 3. 敵スポーンルーチンの開始
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            if (activeMap != null && activeMap.MapData != null)
            {
                spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }

        /// <summary>
        /// 敵が撃破された際に EnemyController 等から呼び出してもらう通知関数
        /// </summary>
        public void OnEnemyKilled()
        {
            if (isPortalSpawned || activeMap == null || activeMap.MapData == null) return;

            currentKillCount++;
            Debug.Log($"[EnemySpawner] 撃破数: {currentKillCount} / {activeMap.MapData.targetKillCount}");

            if (currentKillCount >= activeMap.MapData.targetKillCount)
            {
                isPortalSpawned = true;
                activeMap.SpawnClearPortal();
            }
        }

        private IEnumerator SpawnRoutine()
        {
            var mapData = activeMap.MapData;

            while (true)
            {
                yield return new WaitForSeconds(mapData.spawnInterval);

                activeEnemies.RemoveAll(enemy => enemy == null);

                // 最大生存制限に達している場合はスポーンしない
                if (activeEnemies.Count >= mapData.maxEnemyCount)
                {
                    continue;
                }

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

        private bool TryGetAutoNavMeshSpawnPosition(out Vector3 result)
        {
            result = Vector3.zero;
            if (playerTransform == null || activeMap == null) return false;

            for (int i = 0; i < 15; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 candidatePos = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
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

            if (totalWeight <= 0) return null;

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