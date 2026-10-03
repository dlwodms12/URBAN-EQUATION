using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageGoalRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text description;
    [SerializeField] private Image background;
    [SerializeField] private Sprite achievedSprite;
    [SerializeField] private Sprite pendingSprite;
    public bool IsAchieved { get; private set; }

    public void Show(string text, bool achieved)
    {
        IsAchieved = achieved;
        if (description != null)
        { description.text = text ?? string.Empty; description.fontStyle = achieved ? FontStyles.Strikethrough : FontStyles.Normal; }
        if (background != null) background.sprite = achieved ? achievedSprite : pendingSprite;
    }
}
