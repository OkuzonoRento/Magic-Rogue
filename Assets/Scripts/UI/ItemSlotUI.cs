using UnityEngine;
using UnityEngine.EventSystems;

namespace MagicRogue
{
    public enum SlotType
    {
        Inventory,  // インベントリ
        ShopSlot,   // ショップ商品枠
        BuyPending, // Buy（購入待機）枠
        SellSlot,   // Sell（売却）枠
        AttackSlot  // Attack（魔法）枠
    }

    public class ItemSlotUI : MonoBehaviour, IDropHandler
    {
        [Header("スロット設定")]
        public SlotType slotType = SlotType.Inventory;

        [Tooltip("自動生成時に割り当てられるインデックス（手動設定も可能）")]
        public int slotIndex = 0;

        [Header("見た目の配置設定")]
        [Tooltip("ドロップしたアイコンを配置したい場所（未指定の場合はこのTransform自体）")]
        [SerializeField] private Transform iconTransform;

        private void Awake()
        {
            // iconTransform が未設定の場合は自分自身を配置先に指定
            if (iconTransform == null)
            {
                iconTransform = transform;
            }
        }

        /// <summary>
        /// アイコンを実際に配置すべき Transform を取得
        /// </summary>
        public Transform IconTransform => iconTransform != null ? iconTransform : transform;

        /// <summary>
        /// 自動生成時にインデックスとタイプをセットする初期化メソッド
        /// </summary>
        public void SetupSlot(SlotType type, int index)
        {
            slotType = type;
            slotIndex = index;
        }

        public void OnDrop(PointerEventData eventData)
        {
            GameObject droppedObj = eventData.pointerDrag;
            if (droppedObj == null) return;

            DraggableItemUI draggableItem = droppedObj.GetComponent<DraggableItemUI>();
            if (draggableItem == null) return;

            ItemSlotUI fromSlot = draggableItem.parentAfterDrag.GetComponent<ItemSlotUI>();
            if (fromSlot == null || fromSlot == this) return;

            // ドロップ条件のバリデーションチェック
            if (!CanDrop(fromSlot, this, draggableItem.CurrentItemData))
            {
                Debug.Log($"[Drop Validation] {fromSlot.slotType} から {this.slotType} への移動は許可されていません。");
                return;
            }

            // データの移動・入れ替え処理の実行
            HandleSlotDataSwap(fromSlot, this);

            // ★アイコンの配置先を IconTransform（丸アイコンの位置）に変更
            draggableItem.parentAfterDrag = IconTransform;
        }

        /// <summary>
        /// ドロップ条件の判定ロジック
        /// </summary>
        private bool CanDrop(ItemSlotUI from, ItemSlotUI to, ItemData itemData)
        {
            SlotType fromType = from.slotType;
            SlotType toType = to.slotType;

            // --- 0. AttackSlotへの投入制限（MagicDataのみ可） ---
            if (toType == SlotType.AttackSlot)
            {
                if (!(itemData is MagicData))
                {
                    Debug.Log("[Drop Filter] AttackSlotには魔法（MagicData）のみセット可能です。");
                    return false;
                }
            }

            // --- 1. Inventory / AttackSlot >> BuyPending へのドロップ不可 ---
            if ((fromType == SlotType.Inventory || fromType == SlotType.AttackSlot) && toType == SlotType.BuyPending)
                return false;

            // --- 2. ShopSlot >> Inventory / AttackSlot / SellSlot への直接ドロップ不可 ---
            if (fromType == SlotType.ShopSlot && (toType == SlotType.Inventory || toType == SlotType.AttackSlot || toType == SlotType.SellSlot))
                return false;

            // --- 3. ShopSlot >> BuyPending は許可 ---
            if (fromType == SlotType.ShopSlot && toType == SlotType.BuyPending)
                return true;

            // --- 4. BuyPending <<>> SellSlot の相互ドロップ不可 ---
            if ((fromType == SlotType.BuyPending && toType == SlotType.SellSlot) || (fromType == SlotType.SellSlot && toType == SlotType.BuyPending))
                return false;

            // --- 5. BuyPending >> Inventory / AttackSlot への移動（購入決定前キャンセル） ---
            if (fromType == SlotType.BuyPending)
                return false;

            // --- 6. Inventory, AttackSlot, SellSlot 相互間の移動・入れ替えは許可 ---
            bool isFromValid = (fromType == SlotType.Inventory || fromType == SlotType.AttackSlot || fromType == SlotType.SellSlot);
            bool isToValid = (toType == SlotType.Inventory || toType == SlotType.AttackSlot || toType == SlotType.SellSlot);

            if (isFromValid && isToValid)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 実際のデータ変更（InventorySOやShopManagerの更新）
        /// </summary>
        private void HandleSlotDataSwap(ItemSlotUI fromSlot, ItemSlotUI toSlot)
        {
            if (ShopManager.Instance == null) return;
            InventorySO inventory = ShopManager.Instance.GetPlayerInventory();
            if (inventory == null) return;

            // A. ShopSlot >> BuyPending（購入準備）
            if (fromSlot.slotType == SlotType.ShopSlot && toSlot.slotType == SlotType.BuyPending)
            {
                MagicData shopMagic = ShopManager.Instance.GetShopSlotMagic(fromSlot.slotIndex);
                ShopManager.Instance.SetBuyPendingMagic(shopMagic, fromSlot.slotIndex);
            }
            // B. Inventory / AttackSlot <<>> SellSlot
            else if (fromSlot.slotType == SlotType.SellSlot || toSlot.slotType == SlotType.SellSlot)
            {
                HandleSellSlotSwap(fromSlot, toSlot, inventory);
            }
            // C. Inventory <<>> AttackSlot 相互移動・入れ替え
            else if ((fromSlot.slotType == SlotType.Inventory && toSlot.slotType == SlotType.AttackSlot) ||
                     (fromSlot.slotType == SlotType.AttackSlot && toSlot.slotType == SlotType.Inventory))
            {
                HandleInventoryAttackSwap(fromSlot, toSlot, inventory);
            }
            // D. Inventory <<>> Inventory 入れ替え
            else if (fromSlot.slotType == SlotType.Inventory && toSlot.slotType == SlotType.Inventory)
            {
                ItemStack temp = inventory.inventorySlots[fromSlot.slotIndex];
                inventory.inventorySlots[fromSlot.slotIndex] = inventory.inventorySlots[toSlot.slotIndex];
                inventory.inventorySlots[toSlot.slotIndex] = temp;
            }
            // E. AttackSlot <<>> AttackSlot 入れ替え
            else if (fromSlot.slotType == SlotType.AttackSlot && toSlot.slotType == SlotType.AttackSlot)
            {
                MagicData temp = inventory.spellSlots[fromSlot.slotIndex];
                inventory.spellSlots[fromSlot.slotIndex] = inventory.spellSlots[toSlot.slotIndex];
                inventory.spellSlots[toSlot.slotIndex] = temp;
            }

            // 全体UI更新
            if (ShopUIController.Instance != null)
            {
                ShopUIController.Instance.UpdateShopUI();
            }
        }

        private void HandleSellSlotSwap(ItemSlotUI fromSlot, ItemSlotUI toSlot, InventorySO inventory)
        {
            // Inventory/AttackSlot >> SellSlot
            if (toSlot.slotType == SlotType.SellSlot)
            {
                if (fromSlot.slotType == SlotType.Inventory)
                {
                    ItemStack itemStack = inventory.inventorySlots[fromSlot.slotIndex];
                    if (itemStack != null && itemStack.itemData != null)
                    {
                        ShopManager.Instance.SetSellSlotItem(itemStack.itemData, itemStack.amount, fromSlot.slotIndex);
                        inventory.RemoveItemFromSlot(fromSlot.slotIndex);
                    }
                }
                else if (fromSlot.slotType == SlotType.AttackSlot)
                {
                    MagicData magic = inventory.spellSlots[fromSlot.slotIndex];
                    if (magic != null)
                    {
                        ShopManager.Instance.SetSellSlotItem(magic, 1, -1);
                        inventory.spellSlots[fromSlot.slotIndex] = null;
                    }
                }
            }
            // SellSlot >> Inventory/AttackSlot（売却前キャンセル）
            else if (fromSlot.slotType == SlotType.SellSlot)
            {
                ItemData sellItem = ShopManager.Instance.GetSellSlotItem();
                int sellTotalAmount = ShopManager.Instance.GetSellSlotTotalAmount();

                if (sellItem != null && sellTotalAmount > 0)
                {
                    if (toSlot.slotType == SlotType.Inventory)
                    {
                        inventory.AddItemToSlot(sellItem, sellTotalAmount, toSlot.slotIndex);
                    }
                    else if (toSlot.slotType == SlotType.AttackSlot && sellItem is MagicData magic)
                    {
                        inventory.spellSlots[toSlot.slotIndex] = magic;
                    }

                    ShopManager.Instance.SetSellSlotItem(null, 0, -1);
                }
            }
        }

        private void HandleInventoryAttackSwap(ItemSlotUI fromSlot, ItemSlotUI toSlot, InventorySO inventory)
        {
            if (fromSlot.slotType == SlotType.Inventory && toSlot.slotType == SlotType.AttackSlot)
            {
                ItemStack invItem = inventory.inventorySlots[fromSlot.slotIndex];
                MagicData attackMagic = inventory.spellSlots[toSlot.slotIndex];

                if (invItem != null && invItem.itemData is MagicData newMagic)
                {
                    inventory.spellSlots[toSlot.slotIndex] = newMagic;
                    if (attackMagic != null)
                        inventory.AddItemToSlot(attackMagic, 1, fromSlot.slotIndex);
                    else
                        inventory.RemoveItemFromSlot(fromSlot.slotIndex);
                }
            }
            else if (fromSlot.slotType == SlotType.AttackSlot && toSlot.slotType == SlotType.Inventory)
            {
                MagicData attackMagic = inventory.spellSlots[fromSlot.slotIndex];
                ItemStack invItem = inventory.inventorySlots[toSlot.slotIndex];

                if (attackMagic != null)
                {
                    inventory.spellSlots[fromSlot.slotIndex] = (invItem != null && invItem.itemData is MagicData m) ? m : null;
                    inventory.AddItemToSlot(attackMagic, 1, toSlot.slotIndex);
                }
            }
        }
    }
}