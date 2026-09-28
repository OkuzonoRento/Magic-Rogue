#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MagicRogue
{
    public static class MapBuffDataGenerator
    {
        [MenuItem("MagicRogue/Create All Map Buff Assets")]
        public static void CreateAllMapBuffs()
        {
            // 保存先フォルダのパス（プロジェクト構造に合わせて変更可能です）
            string mapBuffFolderPath = "Assets/DB/Buff/Map";

            // フォルダが存在しない場合は自動作成
            if (!Directory.Exists(mapBuffFolderPath))
            {
                Directory.CreateDirectory(mapBuffFolderPath);
            }
            AssetDatabase.Refresh();

            // Mapバフ定義リスト: (Type, ファイル名, 表示名, 効果値, 持続時間, 説明文)
            var mapBuffList = new (BuffType type, string fileName, string name, float value, float duration, string desc)[]
            {
                (BuffType.StationaryTurret, "MB_StationaryTurret", "固定砲台", 0.4f, 99999f, "停止中に攻撃力が大幅に上昇する"),
                (BuffType.Overload,         "MB_Overload",         "オーバーロード", 0.5f, 99999f, "前半20秒間強化、後半弱体化する"),
                (BuffType.NoonPower,        "MB_NoonPower",        "正午の力", 0.5f, 99999f, "一定時間ごとに攻撃力が変動する"),
                (BuffType.Vengeance,        "MB_Vengeance",        "復讐", 0.3f, 99999f, "被弾時、一時的にステータスが上昇する"),
                (BuffType.CurseStaff,       "MB_CurseStaff",       "呪いの杖", 0.2f, 99999f, "魔法ヒット時、敵を弱体化させる"),
                (BuffType.DivineProtection, "MB_DivineProtection", "神加護", 1.0f, 99999f, "攻撃を無効化するシールドを展開する"),
                (BuffType.GraveRobber,      "MB_GraveRobber",      "墓あらし", 0.25f, 99999f, "回収範囲UP。アイテム拾い時に攻撃力上昇"),
                (BuffType.Stepper,          "MB_Stepper",          "ステッパー", 0.2f, 99999f, "移動中に攻撃力が上昇する"),
                (BuffType.FrogInAWell,      "MB_FrogInAWell",      "井の中の蛙", 0.5f, 99999f, "スロット一致で超強化、不一致で弱体化"),
                (BuffType.PathToAscension,  "MB_PathToAscension",  "修羅の道", 2.0f, 99999f, "初期大幅弱体化。一定数撃破で無敵覚醒"),
                (BuffType.LastStand,        "MB_LastStand",        "背水の陣", 0.8f, 99999f, "HP20%以下で攻撃力が大幅に上昇する"),
                (BuffType.MagicCirculation, "MB_MagicCirculation", "魔力循環", 0.05f, 99999f, "連続ヒットで攻撃速度とCD短縮"),
                (BuffType.FleshCut,         "MB_FleshCut",         "肉を斬らせて", 0.3f, 99999f, "リスクと引き換えに爆発的な火力を得る")
            };

            int createdCount = 0;

            foreach (var b in mapBuffList)
            {
                string assetPath = $"{mapBuffFolderPath}/{b.fileName}.asset";

                // 既にファイルが存在する場合は上書きせずスキップ
                if (File.Exists(assetPath)) continue;

                BuffData buff = ScriptableObject.CreateInstance<BuffData>();
                buff.buffName = b.name;
                buff.buffType = b.type;
                buff.value = b.value;
                buff.duration = b.duration; // Mapバフなのでマップ内無期限(99999s)
                buff.description = b.desc;

                AssetDatabase.CreateAsset(buff, assetPath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MapBuffGenerator] {createdCount} 個のMapバフアセットを '{mapBuffFolderPath}' に生成しました。");
        }
    }
}
#endif