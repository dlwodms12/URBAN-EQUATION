using UnityEngine;
using UnityEngine.InputSystem;

// Scene-local cameras/input/views bind to one persistent session.
public class GameSceneEntry : MonoBehaviour
{
    [SerializeField] private GameApplicationSettings settings;
    [SerializeField] private GameFlowScene destination;
    [SerializeField] private Camera boardCamera;
    [SerializeField] private bool temporarySave;
    [SerializeField] private bool frameBoard = true;
    private GameApplication application;
    private bool started;
    public void Configure(GameApplicationSettings data, GameFlowScene scene, Camera camera, bool temporary)
    { if (started) return; settings = data; destination = scene; boardCamera = camera; temporarySave = temporary; }
    private void Start()
    {
        if (!GameApplication.TryGetOrCreate(settings, temporarySave, out application, out string error))
        { Debug.LogError("Urban Equation scene startup failed: " + error, this); return; }
        started = true;
        var screens = Instantiate(settings.ScreensPrefab);
        screens.Bind(application.Bootstrap.Flow, application.Router);
        if (destination == GameFlowScene.Game)
        {
            var placement = gameObject.AddComponent<BuildingPlacementController>();
            placement.Configure(application.Bootstrap.Session, boardCamera);
            var hud = Instantiate(settings.Hud.HudPrefab);
            hud.SetScreenBackdropVisible(true);
            hud.Bind(application.Bootstrap.Session, application.Bootstrap.Flow, placement, boardCamera);
        }
        application.Router.NotifySceneReady(destination);
    }
    private void Update()
    {
        if (!started || application == null) return;
        if (application.Router.IsReady && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            application.Bootstrap.Flow.TryHandleEscape(out _);
        if (destination != GameFlowScene.Game || boardCamera == null || !frameBoard) return;
        var board = application.Bootstrap.Session.Board;
        var center = new Vector3((board.Width - 1) * .5f, 0, (board.Height - 1) * .5f);
        boardCamera.transform.position = center + new Vector3(6,8,-6);
        boardCamera.transform.LookAt(center);
        boardCamera.orthographicSize = Mathf.Max(4, Mathf.Max(board.Width,board.Height) * .8f);
    }
}
