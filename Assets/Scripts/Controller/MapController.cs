using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    public class MapController : MonoBehaviour
    {
        [Header("マップ設定")]
        [SerializeField] private MapData mapData;

        [Header("プレイヤースポーン位置")]
        [Tooltip("このマップでプレイヤーを配置する初期位置")]
        [SerializeField] private Transform playerSpawnPoint;

        [Header("NavMesh Surface (オプション)")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        public MapData MapData => mapData;
        public Transform PlayerSpawnPoint => playerSpawnPoint;

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
    }
}