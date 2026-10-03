using UnityEngine;
using UnityEngine.UI;

// Opt-in preview wiring; final HUD and scenes are subsequent Phase4 steps.
public class GameplayPrefabPreview : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private GameplayPrefabSet prefabs;
    [SerializeField] private Camera boardCamera;
    private SceneFlowManager flow;
    private GameObject canvasRoot;
    private GameObject handPanel;
    private ComboPopupUI comboPopup;
    private bool bound;

    public void Configure(GameBootstrap game, GameplayPrefabSet views, Camera camera)
    { bootstrap = game; prefabs = views; boardCamera = camera; }

    private void Update()
    {
        if (bound || bootstrap == null || !bootstrap.IsInitialized || prefabs == null
            || prefabs.BuildingCardPrefab == null || prefabs.ComboPopupPrefab == null) return;
        var session = bootstrap.Session;
        var placement = gameObject.AddComponent<BuildingPlacementController>();
        placement.Configure(session, boardCamera);
        canvasRoot = new GameObject("Prefab Preview Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        handPanel = new GameObject("Card Hand Preview", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        handPanel.transform.SetParent(canvasRoot.transform, false);
        var panelRect = (RectTransform)handPanel.transform;
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = new Vector2(1,0);
        panelRect.pivot = Vector2.zero; panelRect.offsetMin = new Vector2(360,12); panelRect.offsetMax = new Vector2(-20,190);
        handPanel.GetComponent<Image>().color = new Color(0.35f,0.29f,0.48f,0.95f);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(handPanel.transform, false);
        var viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
        var content = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = new Vector2(0,1);
        contentRect.pivot = new Vector2(0,0.5f); contentRect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12,12,12,12); layout.spacing = 12;
        layout.childControlWidth = false; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = handPanel.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect; scroll.content = contentRect; scroll.horizontal = true; scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        handPanel.AddComponent<BuildingHandUI>().Bind(session.Hand, placement, prefabs.BuildingCardPrefab, content.transform);
        comboPopup = Instantiate(prefabs.ComboPopupPrefab, canvasRoot.transform);
        comboPopup.Bind(session.Combos, boardCamera);
        flow = bootstrap.Flow; flow.OnStateChanged += Refresh;
        bound = true; Refresh();
    }

    private void Refresh()
    {
        bool playing = flow != null && flow.State == GameFlowState.Playing;
        if (handPanel != null) handPanel.SetActive(playing);
        if (comboPopup != null) comboPopup.gameObject.SetActive(playing);
    }
    private void OnDestroy()
    { if (flow != null) flow.OnStateChanged -= Refresh; }
}
