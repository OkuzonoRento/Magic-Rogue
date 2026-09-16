using UnityEngine;

namespace MagicRogue
{
    public enum SpawnType
    {
        PlayerPosition,
        TargetPosition,
        ForwardDirection
    }

    public enum MovementType
    {
        Straight,   // 通常魔法（直進）
        Spread,     // 拡散魔法（扇形に複数発射）
        Homing,     // 追尾魔法（最寄りの敵を追尾）
        Laser,      // 貫通魔法（レーザー・多段ヒット）
        Split,      // 散弾魔法（命中時に分裂）
        Boomerang   // ブーメラン魔法（途中で手元へ戻る）
    }

    [CreateAssetMenu(fileName = "NewMagicData", menuName = "MagicRogue/Magic Data")]
    public class MagicData : ItemData
    {
        [Header("魔法基本性能")]
        public float damage = 10f;
        public float cooldown = 1.0f;
        public float projectileSpeed = 10f;
        public float duration = 3.0f;

        [Header("挙動タイプ設定")]
        public SpawnType spawnType = SpawnType.ForwardDirection;
        public MovementType movementType = MovementType.Straight;
        public GameObject projectilePrefab;

        [Header("拡散・散弾設定（Spread / Split）")]
        [Tooltip("一度に発射する弾数")]
        public int projectileCount = 3;
        [Tooltip("扇形の拡散角度（度）")]
        public float spreadAngle = 45f;

        [Header("散弾設定（Split）")]
        [Tooltip("残り分裂可能回数（デフォルト2回）")]
        public int maxSplitCount = 2;
        [Tooltip("分裂時に飛ばす方向数（デフォルト4方向）")]
        public int splitSubProjectiles = 4;

        [Header("レーザー設定（Laser）")]
        [Tooltip("ダメージを与える間隔（秒）")]
        public float laserDamageInterval = 0.2f;

        [Header("ブーメラン設定（Boomerang）")]
        [Tooltip("折り返すまでの時間（秒）")]
        public float returnTime = 1.0f;
        [Tooltip("戻る時の誘導速度倍率")]
        public float returnSpeedMultiplier = 1.5f;

        [Header("ビジュアル＆エフェクト (VFX)")]
        public GameObject castEffectPrefab;
        public GameObject hitEffectPrefab;
        public AudioClip castSound;
        public AudioClip hitSound;

        [Header("貫通属性")]
        public bool piercesEnemy = false;
        public bool piercesWall = false;

        private void OnEnable()
        {
            maxStackSize = 1;
        }
    }
}