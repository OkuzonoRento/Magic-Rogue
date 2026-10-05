using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicRogue
{
    public enum MapType
    {
        Normal, // 通常戦闘
        Event,  // イベント（敵大量発生など）
        Boss    // ボス
    }

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
        [Header("マップ種別・選択画面用情報")]
        [Tooltip("マップの種別（Normal / Event / Boss）")]
        public MapType mapType = MapType.Normal;

        [Tooltip("遷移先のシーン名・アセット名（例: Plain）")]
        public string mapName;

        [Tooltip("UI表示名（例: 深淵の森）")]
        public string displayName;

        [Tooltip("選択画面で回転表示する3Dモデルのプレハブ")]
        public GameObject model3DPrefab;

        [TextArea]
        [Tooltip("選択画面で表示するマップの説明文")]
        public string description;

        [Header("スポーン設定")]
        [Tooltip("このマップに出現する敵とその確率リスト")]
        public List<EnemySpawnConfig> spawnableEnemies = new List<EnemySpawnConfig>();

        [Tooltip("同時に存在できる最大敵数")]
        public int maxEnemyCount = 20;

        [Tooltip("スポーン間隔（秒）")]
        public float spawnInterval = 3f;

        [Header("クリア・ポータル設定")]
        [Tooltip("ステージクリアに必要な敵撃破数")]
        public int targetKillCount = 15;

        [Tooltip("クリア時に出現するポータルのプレハブ")]
        public GameObject portalPrefab;

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 上で値が変更されたときに自動実行されるメソッド
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(mapName)) return;

            // 実行中（Playモード）やアセットのパスが取得できない場合はリネームしない
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath)) return;

            // 現在のアセット名を取得
            string currentAssetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // mapName とアセット名が異なる場合のみリネームを実行
            if (currentAssetName != mapName)
            {
                // 次のフレームで安全にリネーム処理を行うよう遅延実行
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;

                    string path = AssetDatabase.GetAssetPath(this);
                    if (!string.IsNullOrEmpty(path))
                    {
                        string result = AssetDatabase.RenameAsset(path, mapName);
                        if (string.IsNullOrEmpty(result))
                        {
                            AssetDatabase.SaveAssets();
                        }
                    }
                };
            }
        }
#endif
    }
}