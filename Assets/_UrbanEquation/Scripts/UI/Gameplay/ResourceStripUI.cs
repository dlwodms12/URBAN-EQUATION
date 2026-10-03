using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceStripUI : MonoBehaviour
{
    [SerializeField] private ResourceIconSet icons;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 24;
    private readonly List<GameObject> rows = new List<GameObject>();
    public int VisibleCount { get; private set; }
    public static readonly Color GainColor = new Color(0, 0.72f, 0.35f);
    public static readonly Color SpendColor = new Color(0.94f, 0.30f, 0.33f);

    public void Show(IReadOnlyList<ResourceAmount> values, bool signed)
    {
        VisibleCount = values == null ? 0 : values.Count;
        while (rows.Count < VisibleCount)
        {
            var row = new GameObject("Resource", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(transform, false);
            var layout = row.GetComponent<LayoutElement>(); layout.preferredWidth = 78; layout.preferredHeight = 38;
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(row.transform, false);
            var rect = (RectTransform)icon.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(0, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(27, 27);
            icon.GetComponent<Image>().preserveAspect = true; icon.GetComponent<Image>().raycastTarget = false;
            var label = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(row.transform, false);
            var textRect = (RectTransform)label.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(29, 0); textRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize;
            text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            rows.Add(row);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            bool active = i < VisibleCount; rows[i].SetActive(active); if (!active) continue;
            ResourceAmount value = values[i]; int sign = signed ? value.Amount.CompareTo(0) : 0;
            Image image = rows[i].GetComponentInChildren<Image>(); image.sprite = icons == null ? null : icons.GetIcon(value.Resource, sign);
            image.enabled = image.sprite != null;
            TMP_Text text = rows[i].GetComponentInChildren<TMP_Text>(); text.text = (signed && value.Amount > 0 ? "+" : "") + value.Amount;
            text.color = signed ? (value.Amount < 0 ? SpendColor : GainColor) : Color.white;
        }
    }
    public void Clear() => Show(null, false);
}
