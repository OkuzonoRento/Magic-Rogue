using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    [Serializable]
    public struct RarityModelConfig
    {
        public ItemRarity rarity;
        public GameObject modelPrefab; // レアリティごとの共通モデル（箱やオーラなど）
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

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            if (popOnSpawn)
            {
                InitPopAnimation();
            }
            else
            {
                basePos = transform.position;
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

        /// <summary>
        /// レアリティまたは個別設定に応じてモデルを生成
        /// </summary>
        private void GenerateWorldModel()
        {
            if (itemData == null) return;

            // 既存の子要素モデルをクリア
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            GameObject modelToSpawn = null;

            // 1. 個別の専用モデルが設定されているか確認
            if (itemData.customWorldModelPrefab != null)
            {
                modelToSpawn = itemData.customWorldModelPrefab;
            }
            // 2. なければレアリティに応じたモデルを取得
            else
            {
                modelToSpawn = GetModelByRarity(itemData.rarity);
            }

            // 3. モデルを生成して子要素にする
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

            // プレイヤーが未取得なら再検索（シーン読み込みタイミング等の対策）
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
                else return; // プレイヤーがいなければ処理をスキップ
            }

            // Y軸自転
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            if (isPopping)
            {
                UpdatePopAnimation();
                return;
            }

            if (playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= magnetRadius)
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