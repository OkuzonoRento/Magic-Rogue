namespace MagicRogue
{
    public enum SlotType
    {
        Inventory,  // インベントリスロット（30枠）
        AttackSlot, // 魔法（攻撃）スロット（3枠）
        ShopSlot,   // ショップ商品スロット（3枠）
        BuyPending, // 購入待機スロット（Buy枠）
        SellSlot    // 売却スロット（Sell枠）
    }
}