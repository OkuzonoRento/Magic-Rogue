using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicRogue
{
    public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [HideInInspector] public Transform parentAfterDrag;
        public ItemData CurrentItemData { get; private set; }

        private Canvas canvas;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetItemData(ItemData data)
        {
            CurrentItemData = data;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            parentAfterDrag = transform.parent;
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.SetParent(parentAfterDrag);
            transform.localPosition = Vector3.zero;
            canvasGroup.blocksRaycasts = true;
        }
    }
}