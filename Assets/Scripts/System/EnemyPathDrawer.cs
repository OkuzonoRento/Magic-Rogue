using UnityEngine;
using UnityEngine.AI;

namespace MagicRogue
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyPathDrawer : MonoBehaviour
    {
        [Header("ˆÚ“®—\‘ªü‚Ì•\¦İ’è")]
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = Color.cyan;
        [SerializeField] private float nodeSphereRadius = 0.15f;

        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnDrawGizmos()
        {
            if (!showPath) return;

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (agent != null && agent.hasPath)
            {
                Gizmos.color = pathColor;
                Vector3[] corners = agent.path.corners;

                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                    Gizmos.DrawSphere(corners[i + 1], nodeSphereRadius);
                }
            }
        }
    }
}