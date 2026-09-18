#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MagicRogue
{
    public static class BuffDataGenerator
    {
        [MenuItem("MagicRogue/Create All Buff Assets")]
        public static void CreateAllBuffs()
        {
            string buffFolderPath = "Assets/DB/Buff/Global/Buffs";
            string debuffFolderPath = "Assets/DB/Buff/Global/Debuffs";

            // フォルダが存在しない場合は自動作成
            if (!Directory.Exists(buffFolderPath)) Directory.CreateDirectory(buffFolderPath);
            if (!Directory.Exists(debuffFolderPath)) Directory.CreateDirectory(debuffFolderPath);
            AssetDatabase.Refresh();

            // 定義リスト: (Type, 接頭辞付きファイル名, 表示名, 効果値, 持続時間, 説明, isBuff)
            var buffList = new (BuffType type, string fileName, string name, float value, float duration, string desc, bool isBuff)[]
            {
                // --- バフ (GB_) ---
                (BuffType.AttackUp, "GB_AttackUp", "攻撃力増加", 0.2f, 10f, "攻撃力が20%増加する", true),
                (BuffType.MaxHpUp, "GB_MaxHpUp", "最大体力増加", 0.2f, 30f, "最大HPが20%増加する", true),
                (BuffType.CooldownReduction, "GB_CooldownReduction", "クールタイム減少", 0.2f, 10f, "魔法のクールタイムが20%減少する", true),
                (BuffType.DropRateUp, "GB_DropRateUp", "ドロップ率増加", 0.5f, 20f, "アイテムドロップ率が50%増加する", true),
                (BuffType.SearchRangeUp, "GB_SearchRangeUp", "サーチ範囲増加", 0.3f, 15f, "敵やアイテムの検索範囲が30%広がる", true),
                (BuffType.EnemyStatDown, "GB_EnemyStatDown", "敵ステータス低下", 0.15f, 10f, "範囲内の敵ステータスを15%低下させる", true),

                // --- デバフ (GD_) ---
                (BuffType.AttackDown, "GD_AttackDown", "攻撃力低下", 0.2f, 8f, "攻撃力が20%低下する", false),
                (BuffType.CooldownIncrease, "GD_CooldownIncrease", "クールタイム増加", 0.3f, 8f, "魔法のクールタイムが30%増加する", false),
                (BuffType.MagicSlotReduction, "GD_MagicSlotReduction", "魔法スロット減少", 1f, 10f, "使用可能な魔法スロットが1つ減る", false),
                (BuffType.SelfDamageOnAttack, "GD_SelfDamageOnAttack", "攻撃自傷", 5f, 10f, "攻撃時に5の自傷ダメージを受ける", false),
                (BuffType.DamageReceivedUp, "GD_DamageReceivedUp", "被ダメージ増加", 0.25f, 8f, "受けるダメージが25%増加する", false),
                (BuffType.ChanceToFail, "GD_ChanceToFail", "確率不発", 0.2f, 6f, "20%の確率で魔法の発動に失敗する", false),
                (BuffType.EnemyStatUp, "GD_EnemyStatUp", "敵ステータス向上", 0.2f, 10f, "敵のステータスが20%上昇する", false),
                (BuffType.SearchRangeDown, "GD_SearchRangeDown", "サーチ範囲低下", 0.3f, 10f, "サーチ範囲が30%狭まる", false)
            };

            int createdCount = 0;

            foreach (var b in buffList)
            {
                string targetFolder = b.isBuff ? buffFolderPath : debuffFolderPath;
                string assetPath = $"{targetFolder}/{b.fileName}.asset";

                if (File.Exists(assetPath)) continue;

                BuffData buff = ScriptableObject.CreateInstance<BuffData>();
                buff.buffName = b.name;
                buff.buffType = b.type;
                buff.value = b.value;
                buff.duration = b.duration;
                buff.description = b.desc;

                AssetDatabase.CreateAsset(buff, assetPath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BuffGenerator] {createdCount} 個のアセットを各フォルダに生成しました。");
        }
    }
}
#endif