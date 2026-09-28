using UnityEngine;

namespace MagicRogue
{
    public class HandController : MonoBehaviour
    {
        public static HandController Instance { get; private set; }

        private ItemData grabbingItem;
        private bool isDropped;

        public SlotType DragSourceType { get; private set; }
        public int DragIndex { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetGrabbingItem(ItemData item) => grabbingItem = item;
        public ItemData GetGrabbingItem() => grabbingItem;
        public bool IsHavingItem() => grabbingItem != null;

        public void SetDragSource(SlotType type, int index)
        {
            DragSourceType = type;
            DragIndex = index;
        }

        public void SetDropped(bool value) => isDropped = value;
        public bool IsDropped() => isDropped;

        public void Clear()
        {
            grabbingItem = null;
            isDropped = false;
            DragIndex = -1;
        }
    }
}