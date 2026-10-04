using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "GameUiUpdate", menuName = "Urban Equation/Game UI Update")]
public class GameUiUpdateSettings : ScriptableObject
{
    [SerializeField] private Sprite lobbyBackground;
    [SerializeField] private Sprite complaintBackground;
    [SerializeField] private Sprite complaintHeader;
    [SerializeField] private Sprite leftScrollButton;
    [SerializeField] private Sprite rightScrollButton;
    [SerializeField] private Sprite stageScrollTrack;
    [SerializeField] private Sprite stageScrollHandle;
    public Sprite LobbyBackground => lobbyBackground;
    public Sprite ComplaintBackground => complaintBackground;
    public Sprite ComplaintHeader => complaintHeader;
    public Sprite LeftScrollButton => leftScrollButton;
    public Sprite RightScrollButton => rightScrollButton;

    public void Validate(List<string> errors)
    {
        foreach (Sprite sprite in new[] { lobbyBackground, complaintBackground, complaintHeader,
            leftScrollButton, rightScrollButton, stageScrollTrack, stageScrollHandle })
            if (sprite == null) errors.Add("UI update requires all seven sprites.");
    }

    public void ConfigureStageScroll(ScrollRect scroll)
    {
        if (scroll == null) return;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 40;
        if (scroll.verticalScrollbar != null) return;
        var track = new GameObject("Stage Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        track.transform.SetParent(scroll.transform, false);
        var rect = (RectTransform)track.transform;
        // Overlay in the panel's existing right gutter; authored viewport and rows stay in place.
        rect.anchorMin = new Vector2(1, .08f); rect.anchorMax = new Vector2(1, .83f);
        rect.pivot = new Vector2(1, .5f); rect.anchoredPosition = new Vector2(-12, 0);
        rect.sizeDelta = new Vector2(18, 0);
        var image = track.GetComponent<Image>(); image.sprite = stageScrollTrack; image.type = Image.Type.Sliced;
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(track.transform, false);
        var handleRect = (RectTransform)handle.transform;
        handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
        var handleImage = handle.GetComponent<Image>(); handleImage.sprite = stageScrollHandle; handleImage.type = Image.Type.Sliced;
        var bar = track.GetComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
        bar.handleRect = handleRect; bar.targetGraphic = handleImage;
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }
}
