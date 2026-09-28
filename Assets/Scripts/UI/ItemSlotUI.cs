using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MagicRogue
{
    public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
    {
        [Header("スロット設定")]
        public SlotType slotType = SlotType.Inventory;
        public int slotIndex = 0;

        [Header("UI参照")]
        [SerializeField] protected Image slotItemImage;
        [SerializeField] protected TextMeshProUGUI itemCountText;

        [Header("ドラッグUI設定")]
        [SerializeField] private GameObject dragItemPrefab;
        private GameObject draggingObject;
        private Transform canvasTransform;

        private void Awake()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) canvasTransform = canvas.transform;
        }

        public void SetupSlot(SlotType type, int index)
        {
            slotType = type;
            slotIndex = index;
            UpdateSlotUI();
        }

        private void OnEnable()
        {
            UpdateSlotUI();
        }

        public void UpdateSlotUI()
        {
            if (ShopManager.Instance == null) return;
            InventorySO inventory = ShopManager.Instance.GetPlayerInventory();

            ItemData displayItem = null;
            int displayAmount = 0;

            switch (slotType)
            {
                case SlotType.Inventory:
                    if (inventory != null && inventory.inventorySlots != null && slotIndex < inventory.inventorySlots.Length)
                    {
                        var stack = inventory.inventorySlots[slotIndex];
                        if (stack != null && stack.itemData != null)
                        {
                            displayItem = stack.itemData;
                            displayAmount = stack.amount;
                        }
                    }
                    break;

                case SlotType.AttackSlot:
                    if (inventory != null && inventory.spellSlots != null && slotIndex < inventory.spellSlots.Length)
                    {
                        displayItem = inventory.spellSlots[slotIndex];
                        displayAmount = 1;
                    }
                    break;

                case SlotType.ShopSlot:
                    displayItem = ShopManager.Instance.GetShopSlotMagic(slotIndex);
                    displayAmount = 1;
                    break;

                case SlotType.BuyPending:
                    displayItem = ShopManager.Instance.GetBuyPendingMagic();
                    displayAmount = 1;
                    break;

                case SlotType.SellSlot:
                    displayItem = ShopManager.Instance.GetSellSlotItem();
                    displayAmount = ShopManager.Instance.GetSellSlotTotalAmount();
                    break;
            }

            // 表示の反映
            if (displayItem != null && displayItem.icon != null)
            {
                if (slotItemImage != null)
                {
                    slotItemImage.sprite = displayItem.icon;
                    slotItemImage.color = Color.white;
                }
                if (itemCountText != null)
                {
                    itemCountText.text = displayAmount > 1 ? $"x{displayAmount}" : "";
                }
            }
            else
            {
                if (slotItemImage != null)
                {
                    slotItemImage.sprite = null;
                    slotItemImage.color = Color.clear;
                }
                if (itemCountText != null)
                {
                    itemCountText.text = "";
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ItemData currentItem = GetCurrentItemData();
            if (currentItem == null) return;

            if (HandController.Instance != null)
            {
                HandController.Instance.SetDropped(false);
                HandController.Instance.SetDragSource(slotType, slotIndex);
                HandController.Instance.SetGrabbingItem(currentItem);
            }

            if (dragItemPrefab != null && canvasTransform != null)
            {
                draggingObject = Instantiate(dragItemPrefab, canvasTransform);
                draggingObject.transform.SetAsLastSibling();

                Image dragImage = draggingObject.GetComponent<Image>();
                if (dragImage == null) dragImage = draggingObject.GetComponentInChildren<Image>();

                if (dragImage != null)
                {
                    dragImage.sprite = currentItem.icon;
                    dragImage.color = Color.white;
                    dragImage.raycastTarget = false;
                }
            }

            if (slotItemImage != null) slotItemImage.color = new Color(1f, 1f, 1f, 0.4f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggingObject != null)
            {
                draggingObject.transform.position = eventData.position;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (HandController.Instance == null || !HandController.Instance.IsHavingItem()) return;

            ItemData grabbingItem = HandController.Instance.GetGrabbingItem();
            SlotType fromType = HandController.Instance.DragSourceType;
            int fromIndex = HandController.Instance.DragIndex;

            if (grabbingItem == null) return;

            if (!CanDrop(fromType, this.slotType, grabbingItem)) return;

            HandleSlotDataSwap(fromType, fromIndex, this.slotType, this.slotIndex, grabbingItem);

            HandController.Instance.SetDropped(true);
            HandController.Instance.Clear();

            ShopManager.Instance?.UpdateAllUI();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggingObject != null)
            {
                Destroy(draggingObject);
            }

            if (HandController.Instance != null)
            {
                if (HandController.Instance.DragSourceType == SlotType.BuyPending && !HandController.Instance.IsDropped())
                {
                    ShopManager.Instance?.ClearBuyPending();
                }
                else if (HandController.Instance.DragSourceType == SlotType.SellSlot && !HandController.Instance.IsDropped())
                {
                    ShopManager.Instance?.ClearSellPending();
                }

                HandController.Instance.Clear();
            }

            ShopManager.Instance?.UpdateAllUI();
        }

        private ItemData GetCurrentItemData()
        {
            if (ShopManager.Instance == null) return null;
            InventorySO inventory = ShopManager.Instance.GetPlayerInventory();

            switch (slotType)
            {
                case SlotType.Inventory:
                    return (inventory != null && slotIndex < inventory.inventorySlots.Length) ? inventory.inventorySlots[slotIndex]?.itemData : null;
                case SlotType.AttackSlot:
                    return (inventory != null && slotIndex < inventory.spellSlots.Length) ? inventory.spellSlots[slotIndex] : null;
                case SlotType.ShopSlot:
                    return ShopManager.Instance.GetShopSlotMagic(slotIndex);
                case SlotType.BuyPending:
                    return ShopManager.Instance.GetBuyPendingMagic();
                case SlotType.SellSlot:
                    return ShopManager.Instance.GetSellSlotItem();
                default:
                    return null;
            }
        }

        private bool CanDrop(SlotType fromType, SlotType toType, ItemData itemData)
        {
            if (toType == SlotType.AttackSlot && !(itemData is MagicData)) return false;
            if ((fromType == SlotType.Inventory || fromType == SlotType.AttackSlot) && toType == SlotType.BuyPending) return false;
            if (fromType == SlotType.ShopSlot && toType != SlotType.BuyPending) return false;
            if (fromType == SlotType.BuyPending && toType == SlotType.SellSlot) return false;

            if (fromType == SlotType.BuyPending) return true;

            return true;
        }

        private void HandleSlotDataSwap(SlotType fromType, int fromIdx, SlotType toType, int toIdx, ItemData item)
        {
            if (ShopManager.Instance == null) return;
            InventorySO inventory = ShopManager.Instance.GetPlayerInventory();

            // BuyPending -> 他スロット
            if (fromType == SlotType.BuyPending)
            {
                ShopManager.Instance.ClearBuyPending();
                return;
            }

            // A. ShopSlot -> BuyPending
            if (fromType == SlotType.ShopSlot && toType == SlotType.BuyPending)
            {
                if (item is MagicData magic)
                {
                    ShopManager.Instance.SetBuyPendingMagic(magic, fromIdx);
                }
            }
            // B. Inventory / AttackSlot -> SellSlot
            else if (toType == SlotType.SellSlot)
            {
                if (fromType == SlotType.Inventory)
                {
                    ItemStack stack = inventory.inventorySlots[fromIdx];
                    if (stack != null && stack.itemData != null)
                    {
                        ShopManager.Instance.SetSellSlotItem(stack.itemData, stack.amount, fromIdx);
                        inventory.RemoveItemFromSlot(fromIdx);
                        // 単純にSell枠へ送っただけ（入れ替えなし）で元の位置が空いたら詰める
                        inventory.CompactInventory();
                    }
                }
                else if (fromType == SlotType.AttackSlot)
                {
                    MagicData magic = inventory.spellSlots[fromIdx];
                    if (magic != null)
                    {
                        ShopManager.Instance.SetSellSlotItem(magic, 1, -1);
                        inventory.spellSlots[fromIdx] = null;
                    }
                }
            }
            // C. SellSlot -> Inventory / AttackSlot (Sell枠とドロップ先アイテムの相互完全入れ替え)
            else if (fromType == SlotType.SellSlot)
            {
                // Sellスロットにあったアイテム情報を抜き出す（勝手なAddItem返還を防止）
                ItemData sellItem = ShopManager.Instance.ExtractSellPendingItem(out int sellAmount);

                if (sellItem != null && sellAmount > 0)
                {
                    if (toType == SlotType.Inventory)
                    {
                        // ドロップ先のインベントリスロット情報を保持
                        ItemStack targetStack = inventory.inventorySlots[toIdx];
                        ItemData targetItem = targetStack != null ? targetStack.itemData : null;
                        int targetAmount = targetStack != null ? targetStack.amount : 0;

                        // ドロップ先指定位置に Sell にあったアイテムを配置
                        inventory.inventorySlots[toIdx] = new ItemStack { itemData = sellItem, amount = sellAmount };

                        // ドロップ先に元々アイテムがあった場合：それをそのまま Sell 枠へ差し替える
                        if (targetItem != null && targetAmount > 0)
                        {
                            ShopManager.Instance.SetSellSlotItem(targetItem, targetAmount, toIdx);
                        }
                        else
                        {
                            // 空きスロットへ置いた場合は詰める
                            inventory.CompactInventory();
                        }
                    }
                    else if (toType == SlotType.AttackSlot && sellItem is MagicData magic)
                    {
                        MagicData targetMagic = inventory.spellSlots[toIdx];

                        inventory.spellSlots[toIdx] = magic;

                        if (sellAmount > 1)
                        {
                            inventory.AddItem(magic, sellAmount - 1);
                        }

                        if (targetMagic != null)
                        {
                            ShopManager.Instance.SetSellSlotItem(targetMagic, 1, -1);
                        }
                    }
                }
            }
            // D. Inventory <-> AttackSlot
            else if ((fromType == SlotType.Inventory && toType == SlotType.AttackSlot) ||
                     (fromType == SlotType.AttackSlot && toType == SlotType.Inventory))
            {
                if (fromType == SlotType.Inventory && toType == SlotType.AttackSlot)
                {
                    ItemStack invItem = inventory.inventorySlots[fromIdx];
                    MagicData currentAttack = inventory.spellSlots[toIdx];

                    if (invItem != null && invItem.itemData is MagicData newMagic)
                    {
                        inventory.spellSlots[toIdx] = newMagic;
                        if (currentAttack != null)
                            inventory.inventorySlots[fromIdx] = new ItemStack { itemData = currentAttack, amount = 1 };
                        else
                        {
                            inventory.RemoveItemFromSlot(fromIdx);
                            inventory.CompactInventory();
                        }
                    }
                }
                else
                {
                    MagicData attackMagic = inventory.spellSlots[fromIdx];
                    ItemStack targetInvStack = inventory.inventorySlots[toIdx];

                    if (attackMagic != null)
                    {
                        if (targetInvStack != null && targetInvStack.itemData is MagicData targetMagic)
                        {
                            inventory.spellSlots[fromIdx] = targetMagic;
                            inventory.inventorySlots[toIdx] = new ItemStack { itemData = attackMagic, amount = 1 };
                        }
                        else
                        {
                            inventory.spellSlots[fromIdx] = (targetInvStack != null && targetInvStack.itemData is MagicData m) ? m : null;
                            inventory.inventorySlots[toIdx] = new ItemStack { itemData = attackMagic, amount = 1 };
                        }
                    }
                }
            }
            // E. Inventory <-> Inventory (相互位置交換)
            else if (fromType == SlotType.Inventory && toType == SlotType.Inventory)
            {
                ItemStack temp = inventory.inventorySlots[fromIdx];
                inventory.inventorySlots[fromIdx] = inventory.inventorySlots[toIdx];
                inventory.inventorySlots[toIdx] = temp;

                // 途中に空きができないように交換後に詰める
                inventory.CompactInventory();
            }
            // F. AttackSlot <-> AttackSlot
            else if (fromType == SlotType.AttackSlot && toType == SlotType.AttackSlot)
            {
                MagicData temp = inventory.spellSlots[fromIdx];
                inventory.spellSlots[fromIdx] = inventory.spellSlots[toIdx];
                inventory.spellSlots[toIdx] = temp;
            }
        }
    }
}