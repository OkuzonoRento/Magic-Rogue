using UnityEngine;

namespace MagicRogue
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickupSystem : MonoBehaviour
    {
        [Header("取得アイテム")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int amount = 1;

        [Header("浮遊アニメーション")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatAmplitude = 0.2f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // PlayerController 等から InventorySO の参照を取得して追加
                if (other.TryGetComponent<PlayerController>(out var player))
                {
                    // ※ InventorySO 側のAddItemメソッドを呼び出し
                    bool added = player.Inventory.AddItem(itemData, amount);

                    if (added)
                    {
                        Debug.Log($"[Pickup] {itemData.itemName} x{amount} をインベントリに追加しました（売却値: {itemData.sellPrice}G）");
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.Log("[Pickup] インベントリが満タンのため拾えません。");
                    }
                }
            }
        }
    }
}