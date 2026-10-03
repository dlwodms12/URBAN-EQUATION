using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image selectIcon;
    [SerializeField] private Sprite playIcon;
    [SerializeField] private Sprite lockIcon;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Image[] stars = new Image[3];
    private Action<int> select;
    public int StageNumber { get; private set; }
    public int BestRank { get; private set; }
    public bool IsUnlocked { get; private set; }
    public void Show(int number, int rank, bool unlocked, bool interactable, Action<int> selected)
    {
        StageNumber = number; BestRank = Mathf.Clamp(rank,0,3); IsUnlocked = unlocked; select = selected;
        if (title != null) title.text = "Stage " + number;
        if (stars != null) for(int i=0;i<stars.Length;i++) if(stars[i]!=null) stars[i].enabled = unlocked && i<BestRank;
        if (selectIcon != null) selectIcon.sprite = unlocked ? playIcon : lockIcon;
        if (selectButton != null)
        {
            selectButton.interactable = unlocked && interactable;
            if (selectButton.targetGraphic is Image image) image.sprite = unlocked ? unlockedSprite : lockedSprite;
            selectButton.onClick.RemoveListener(Selected); selectButton.onClick.AddListener(Selected);
        }
    }
    private void Selected() { if (selectButton != null && selectButton.interactable && IsUnlocked) select?.Invoke(StageNumber); }
    private void OnDestroy() { if(selectButton!=null) selectButton.onClick.RemoveListener(Selected); }
}
