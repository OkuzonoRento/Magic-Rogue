using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public enum GameStateType
    {
        None,
        InBuffSelection, // バフ・マップ選択中
        InGame,          // インゲームプレイ中
        InShop           // ショップ利用中
    }

    [System.Serializable]
    public class SaveData
    {
        public GameStateType stateType = GameStateType.None;

        // --- 選択・マップデータ ---
        public string selectedMapName;
        public List<string> selectedGlobalBuffNames = new List<string>();
        public List<string> selectedMapBuffNames = new List<string>();

        // --- 厳選対策用 ---
        public int randomSeed;                 // 選出乱数シード
        public bool hasConfirmedSelections;   // 選択確定後にインゲームへ入ったか

        // --- インベントリ・プレイヤー保持データ ---
        public float currentHealth;
        public float maxHealth;
        public int playerGold;
        public List<string> inventoryItemIDs = new List<string>(); // 所持アイテム/魔法など
    }
}