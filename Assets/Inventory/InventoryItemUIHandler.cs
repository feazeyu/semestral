using UnityEngine;
using UnityEngine.EventSystems;

namespace RPGFramework.Inventory
{
    public class InventoryItemUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        GameObject originalParent;
        UISlot UISlot;
        Canvas canvas;
        RectTransform rectTransform;
        CanvasGroup canvasGroup;

        Transform dragLayer;

        public void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvas = GetComponentInParent<Canvas>();
            originalParent = transform.parent.gameObject;
            UISlot = originalParent.GetComponent<UISlot>();

            if (UISlot == null)
                Debug.LogWarning($"InventoryItem {gameObject.name} is not in an inventory");

            // Find DragLayer
            var found = canvas.transform.Find("DragLayer");
            if (found != null)
                dragLayer = found;
            else
                Debug.LogError("No DragLayer found under Canvas");
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            originalParent = transform.parent.gameObject;
            if (UISlot.invSlot.RemoveItem())
            {
                transform.SetParent(dragLayer, true);
                canvasGroup.blocksRaycasts = false;
            }
            else { 
                eventData.pointerDrag = null; // Prevents the item from being dragged if it cannot be removed
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint);

                rectTransform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            GameObject target = eventData.pointerEnter;
            UISlot targetSlot = null;

            if (target != null)
                targetSlot = target.GetComponent<UISlot>();

            if (targetSlot != null && targetSlot.invSlot.PutItem(gameObject))
            {
                transform.SetParent(targetSlot.transform, false);
                UISlot = targetSlot;
            }
            else
            {
                transform.SetParent(originalParent.transform, false);
                UISlot.invSlot.PutItem(gameObject); // Put back in original slot
            }
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
