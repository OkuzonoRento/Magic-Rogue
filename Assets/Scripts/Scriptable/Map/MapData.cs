using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    [Serializable]
    public struct EnemySpawnConfig
    {
        [Tooltip("スポーンさせる敵のプレハブ")]
        public GameObject enemyPrefab;

        [Tooltip("出現重み（数字が大きいほど出現しやすい）")]
        [Range(1, 100)]
        public int spawnWeight;
    }

    [CreateAssetMenu(fileName = "NewMapData", menuName = "MagicRogue/Map Data")]
    public class MapData : ScriptableObject
    {
        [Header("マップ基本情報")]
        public string mapName;

        [Header("スポーン設定")]
        [Tooltip("このマップに出現する敵とその確率リスト")]
        public List<EnemySpawnConfig> spawnableEnemies = new List<EnemySpawnConfig>();

        [Tooltip("同時に存在できる最大敵数")]
        public int maxEnemyCount = 20;

        [Tooltip("スポーン間隔（秒）")]
        public float spawnInterval = 3f;
    }
}