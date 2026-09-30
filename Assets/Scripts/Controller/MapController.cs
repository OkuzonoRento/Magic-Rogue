using Unity.AI.Navigation;
using UnityEngine;

namespace MagicRogue
{
    public class MapController : MonoBehaviour
    {
        [Header("マップ設定")]
        [SerializeField] private MapData mapData;

        [Header("スポーン位置設定")]
        [Tooltip("このマップでプレイヤーを配置する初期位置")]
        [SerializeField] private Transform playerSpawnPoint;

        [Tooltip("クリア用ポータルが出現する位置")]
        [SerializeField] private Transform portalSpawnPoint;

        [Header("NavMesh Surface (オプション)")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        public MapData MapData => mapData;
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Transform PortalSpawnPoint => portalSpawnPoint;

        public void ActivateMap()
        {
            gameObject.SetActive(true);

            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }
        }

        public void DeactivateMap()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// クリア条件達成時にポータルを生成する
        /// </summary>
        public void SpawnClearPortal()
        {
            if (mapData == null || mapData.portalPrefab == null)
            {
                Debug.LogWarning("[MapController] ポータルプレハブまたはMapDataが設定されていません。");
                return;
            }

            Vector3 spawnPos = portalSpawnPoint != null ? portalSpawnPoint.position : transform.position;
            Quaternion spawnRot = portalSpawnPoint != null ? portalSpawnPoint.rotation : Quaternion.identity;

            Instantiate(mapData.portalPrefab, spawnPos, spawnRot);
            Debug.Log("[MapController] ステージクリア用ポータルが出現しました！");
        }
    }
}