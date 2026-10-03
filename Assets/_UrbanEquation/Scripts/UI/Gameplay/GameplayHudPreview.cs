using UnityEngine;

public class GameplayHudPreview : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private GameplayHudSet hudSet;
    [SerializeField] private Camera boardCamera;
    private bool bound;
    public void Configure(GameBootstrap game, GameplayHudSet views, Camera camera)
    { bootstrap = game; hudSet = views; boardCamera = camera; }
    private void Update()
    {
        if (bound || bootstrap == null || !bootstrap.IsInitialized || hudSet == null || hudSet.HudPrefab == null) return;
        var placement = gameObject.AddComponent<BuildingPlacementController>(); placement.Configure(bootstrap.Session, boardCamera);
        var hud = Instantiate(hudSet.HudPrefab, transform); hud.Bind(bootstrap.Session, bootstrap.Flow, placement, boardCamera);
        var debug = bootstrap.GetComponentInChildren<GameFlowDebugUI>();
        if (debug != null) { debug.SetManualPlacement(false); debug.SetGameplayPanelVisible(false); }
        bound = true;
    }
}
