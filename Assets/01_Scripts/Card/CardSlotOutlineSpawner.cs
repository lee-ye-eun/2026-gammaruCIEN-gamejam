using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CardSlotOutlineSpawner : MonoBehaviour
{
    [SerializeField] private CardSelectionSlot[] slots;
    [SerializeField] private Image outlineTemplate;

    private const string SpawnedOutlineName = "RuntimeSlotDashedOutline";

    private void Awake()
    {
        SpawnOutlines();
    }

    public void SpawnOutlines()
    {
        if (outlineTemplate == null) return;

        if (slots == null || slots.Length == 0)
        {
            slots = GetComponentsInChildren<CardSelectionSlot>(true);
        }

        foreach (CardSelectionSlot slot in slots)
        {
            if (slot == null) continue;

            RectTransform slotRect = slot.transform as RectTransform;
            if (slotRect == null) continue;

            RemoveExistingOutline(slotRect);
            Image outline = Instantiate(outlineTemplate, slotRect);
            outline.name = SpawnedOutlineName;
            outline.gameObject.SetActive(true);
            outline.raycastTarget = false;

            RectTransform outlineRect = outline.rectTransform;
            outlineRect.SetSiblingIndex(0);
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            outlineRect.pivot = new Vector2(0.5f, 0.5f);
            outlineRect.localScale = Vector3.one;
        }

        outlineTemplate.gameObject.SetActive(false);
    }

    private static void RemoveExistingOutline(RectTransform slotRect)
    {
        for (int i = slotRect.childCount - 1; i >= 0; i--)
        {
            Transform child = slotRect.GetChild(i);
            if (child.name == SpawnedOutlineName)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
