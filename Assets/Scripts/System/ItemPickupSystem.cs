using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    [Serializable]
    public struct RarityModelConfig
    {
        public ItemRarity rarity;
        public GameObject modelPrefab;
    }

    [RequireComponent(typeof(Collider))]
    public class ItemPickupSystem : MonoBehaviour
    {
        [Header("取得アイテム")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int amount = 1;

        [Header("レアリティ別モデル設定")]
        [SerializeField] private List<RarityModelConfig> rarityModelConfigs;

        [Header("浮遊・回転アニメーション")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatAmplitude = 0.2f;

        [Header("出現ポップ演出（放物線）")]
        [SerializeField] private bool popOnSpawn = true;
        [SerializeField] private float popForce = 3f;
        [SerializeField] private float popHeight = 1.5f;

        [Header("吸い込み設定（マグネット）")]
        [SerializeField] private float magnetRadius = 5f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float collectRadius = 0.8f;

        private Transform playerTransform;
        private BuffHandler playerBuffHandler; // 追加: プレイヤーの BuffHandler 参照
        private Vector3 basePos;
        private Vector3 spawnStartPos;
        private Vector3 spawnTargetPos;
        private float popTimer = 0f;
        private float popDuration = 0.4f;
        private bool isPopping = false;
        private bool isMagnetized = false;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;

            FindPlayerReferences();

            if (popOnSpawn)
            {
                InitPopAnimation();
            }
            else
            {
                basePos = transform.position;
            }
        }

        private void FindPlayerReferences()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerBuffHandler = player.GetComponent<BuffHandler>();
            }
        }

        public void Setup(ItemData data, int itemAmount = 1, bool triggerPop = true)
        {
            itemData = data;
            amount = itemAmount;
            popOnSpawn = triggerPop;

            GenerateWorldModel();

            if (popOnSpawn && gameObject.activeInHierarchy)
            {
                InitPopAnimation();
            }
        }

        private void GenerateWorldModel()
        {
            if (itemData == null) return;

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            GameObject modelToSpawn = null;

            if (itemData.customWorldModelPrefab != null)
            {
                modelToSpawn = itemData.customWorldModelPrefab;
            }
            else
            {
                modelToSpawn = GetModelByRarity(itemData.rarity);
            }

            if (modelToSpawn != null)
            {
                Instantiate(modelToSpawn, transform);
            }
        }

        private GameObject GetModelByRarity(ItemRarity rarity)
        {
            if (rarityModelConfigs == null) return null;

            foreach (var config in rarityModelConfigs)
            {
                if (config.rarity == rarity)
                {
                    return config.modelPrefab;
                }
            }
            return null;
        }

        private void InitPopAnimation()
        {
            isPopping = true;
            popTimer = 0f;
            spawnStartPos = transform.position;

            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1f, popForce);
            spawnTargetPos = spawnStartPos + new Vector3(randomDir.x, 0f, randomDir.y);
        }

        private void Update()
        {
            if (itemData == null) return;

            if (playerTransform == null)
            {
                FindPlayerReferences();
                if (playerTransform == null) return;
            }

            // Y軸自転
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            if (isPopping)
            {
                UpdatePopAnimation();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // --- 追加: 【盗掘者 (GraveRobber)】 回収範囲の拡張判定 ---
            float currentMagnetRadius = magnetRadius;
            if (playerBuffHandler != null)
            {
                if (playerBuffHandler.HasBuff(BuffType.GraveRobber))
                {
                    currentMagnetRadius *= 1.8f; // 回収範囲を 80% 拡大
                }
            }

            if (distanceToPlayer <= currentMagnetRadius)
            {
                isMagnetized = true;
            }

            if (isMagnetized)
            {
                Vector3 targetPos = playerTransform.position + Vector3.up * 0.5f;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPos) <= collectRadius)
                {
                    TryCollect();
                }
            }
            else
            {
                float newY = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        private void UpdatePopAnimation()
        {
            popTimer += Time.deltaTime;
            float progress = popTimer / popDuration;

            if (progress >= 1f)
            {
                transform.position = spawnTargetPos;
                basePos = spawnTargetPos;
                isPopping = false;
            }
            else
            {
                Vector3 currentXZ = Vector3.Lerp(spawnStartPos, spawnTargetPos, progress);
                float currentY = Mathf.Sin(progress * Mathf.PI) * popHeight;
                transform.position = new Vector3(currentXZ.x, spawnStartPos.y + currentY, currentXZ.z);
            }
        }

        private void TryCollect()
        {
            if (playerTransform != null && playerTransform.TryGetComponent<PlayerController>(out var player))
            {
                if (player.Inventory != null)
                {
                    bool added = player.Inventory.AddItem(itemData, amount);

                    if (added)
                    {
                        // 追加: 【盗掘者】拾った際の攻撃力UPトリガー実行
                        if (playerBuffHandler != null)
                        {
                            playerBuffHandler.OnItemPickedUp();
                        }

                        Debug.Log($"[Pickup] {itemData.itemName} x{amount} をインベントリに追加しました（売却値: {itemData.sellPrice}G）");
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.Log("[Pickup] インベントリが満タンのため拾えません。");
                        isMagnetized = false;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isPopping && other.CompareTag("Player"))
            {
                TryCollect();
            }
        }
    }
}