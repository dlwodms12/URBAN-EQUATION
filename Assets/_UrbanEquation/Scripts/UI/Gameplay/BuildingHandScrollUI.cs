using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Moves by one card. The actual viewport width determines whether there is overflow.
public class BuildingHandScrollUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    private GameSessionManager session;
    private int heldDirection;
    private float heldTime;
    private bool suppressClick;
    private int observedChildren = -1;
    public Button LeftButton => leftButton;
    public Button RightButton => rightButton;
    public float MaximumOffset => scroll == null || scroll.content == null || scroll.viewport == null
        ? 0 : Mathf.Max(0, scroll.content.rect.width - scroll.viewport.rect.width);
    public float Offset => scroll == null || scroll.content == null ? 0 : -scroll.content.anchoredPosition.x;

    public void Configure(ScrollRect view, Sprite left, Sprite right, GameSessionManager game)
    {
        EndHold(); suppressClick = false; scroll = view; session = game; observedChildren = -1;
        if (scroll == null) return;
        scroll.scrollSensitivity = 0;
        scroll.inertia = false;
        if (leftButton == null) leftButton = CreateButton("Left Scroll", left, false, -1);
        if (rightButton == null) rightButton = CreateButton("Right Scroll", right, true, 1);
        Refresh();
    }
    private bool CanInteract => isActiveAndEnabled && (session == null || (session.GameplayEnabled && !session.IsBusy));
    private Button CreateButton(string name, Sprite sprite, bool right, int direction)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
        root.transform.SetParent(transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(right ? 1 : 0, 0); rect.anchorMax = new Vector2(right ? 1 : 0, 1);
        rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(30, -20);
        rect.anchoredPosition = new Vector2(right ? -22 : 22, 0);
        var image = root.GetComponent<Image>(); image.sprite = sprite;
        var button = root.GetComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(() => { if (suppressClick) suppressClick = false; else Step(direction); });
        var trigger = root.GetComponent<EventTrigger>();
        Add(trigger, EventTriggerType.PointerDown, () => BeginHold(direction));
        Add(trigger, EventTriggerType.PointerUp, EndHold);
        Add(trigger, EventTriggerType.PointerExit, () => { EndHold(); suppressClick = false; });
        return button;
    }
    private static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data =>
        {
            if (type == EventTriggerType.PointerDown && data is PointerEventData pointer
                && pointer.button != PointerEventData.InputButton.Left) return;
            action();
        });
        trigger.triggers.Add(entry);
    }
    public void BeginHold(int direction)
    {
        EndHold();
        if (!CanInteract || (direction != -1 && direction != 1)) return;
        heldDirection = direction; suppressClick = true; Step(direction);
    }
    public void EndHold() { heldDirection = 0; heldTime = 0; }
    public void Tick(float delta)
    {
        if (!CanInteract) { EndHold(); return; }
        if (heldDirection == 0 || float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0) return;
        heldTime += delta;
        while (heldTime >= .5f)
        { heldTime -= .5f; if (!Step(heldDirection)) { EndHold(); break; } }
    }
    public bool Step(int direction)
    {
        if (!CanInteract || scroll == null || scroll.content == null || (direction != -1 && direction != 1)) return false;
        Refresh();
        float stride = 0;
        for (int i = 0; i < scroll.content.childCount; i++)
            if (scroll.content.GetChild(i) is RectTransform card && card.gameObject.activeSelf)
            { stride = card.rect.width; break; }
        if (scroll.content.TryGetComponent<HorizontalLayoutGroup>(out var layout)) stride += layout.spacing;
        if (stride <= 0) return false;
        float before = Offset;
        SetOffset(Mathf.Clamp(before + direction * stride, 0, MaximumOffset));
        Refresh(); return Mathf.Abs(Offset - before) > .001f;
    }
    public void Refresh()
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        if (observedChildren != scroll.content.childCount)
        { observedChildren = scroll.content.childCount; LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content); }
        SetOffset(Mathf.Clamp(Offset, 0, MaximumOffset));
        bool overflow = MaximumOffset > .5f;
        SetButton(leftButton, overflow && Offset > .5f);
        SetButton(rightButton, overflow && Offset < MaximumOffset - .5f);
    }
    private void SetButton(Button button, bool visible)
    { if (button != null) { button.gameObject.SetActive(visible); button.interactable = CanInteract; } }
    private void SetOffset(float offset)
    { var point = scroll.content.anchoredPosition; point.x = -offset; scroll.content.anchoredPosition = point; scroll.StopMovement(); }
    private void LateUpdate() { Refresh(); Tick(Time.unscaledDeltaTime); }
    private void OnDisable() { EndHold(); suppressClick = false; }
}
