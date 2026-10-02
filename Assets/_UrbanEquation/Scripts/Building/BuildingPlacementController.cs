using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacementController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameSessionManager session;
    private BuildingInstance preview;
    private Tile currentTile;
    private Vector2 pointerPosition;
    private int selectedCardId;
    private BuildingCardState selectedCard;
    private bool previewCanPlace;
    private GameSessionManager subscribedSession;

    public bool IsDragging => preview != null;
    public int SelectedCardId => selectedCardId;
    public bool PreviewCanPlace => previewCanPlace;
    public Tile PreviewTile => currentTile;
    public event Action<Tile, bool> OnPreviewChanged;

    public void Configure(GameSessionManager gameSession, Camera camera)
    {
        CancelDrag();
        UnsubscribeSession();
        session = gameSession;
        mainCamera = camera;
        SubscribeSession();
    }

    public bool TryBeginDrag(int cardId)
    {
        if (session == null || session.Hand == null || session.BuildingPrefab == null
            || !session.Hand.IsCardAvailable(cardId)
            || !session.Hand.TryGetCard(cardId, out BuildingCardState card)
            || card.Building.VisualPrefab == null) return false;

        CancelDrag();
        var root = new GameObject("BuildingPreviewCandidate");
        root.SetActive(false);
        try
        {
            preview = Instantiate(session.BuildingPrefab, root.transform);
            preview.Initialize(card.Building, Vector2Int.zero);
            // Include inactive colliders so an enabled child cannot intercept board raycasts.
            foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Rigidbody body in preview.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            preview.transform.SetParent(transform, true);
            selectedCardId = cardId;
            selectedCard = card;
            preview.gameObject.SetActive(true);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not create building preview: " + exception.Message, this);
            CancelDrag();
            return false;
        }
        finally
        {
            DestroyGenerated(root);
        }
        return true;
    }

    // EventSystem supplies pointer coordinates; no dependency on a specific input backend.
    public void UpdatePointer(Vector2 screenPosition)
    {
        pointerPosition = screenPosition;
        if (!IsDragging) return;
        if (session == null || session.Hand == null || !session.Hand.IsCardAvailable(selectedCardId)
            || !session.Hand.TryGetCard(selectedCardId, out BuildingCardState card) || card != selectedCard)
        { CancelDrag(); return; }
        Tile target = null;
        if (mainCamera != null && session.Board != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tile hitTile = hit.collider.GetComponentInParent<Tile>();
                if (hitTile != null && session.Board.GetTile(hitTile.Coordinate) == hitTile)
                    target = hitTile;
            }
            // Dropping over another UI control cancels placement.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                target = null;
            var ground = new Plane(Vector3.up, session.Board.transform.position);
            if (ground.Raycast(ray, out float distance))
                preview.transform.position = ray.GetPoint(distance);
        }
        bool canPlace = target != null && session.CanBuild(selectedCardId, target.Coordinate, out _);
        if (target != null) preview.transform.position = target.transform.position;
        SetTarget(target, canPlace);
    }

    private void Update()
    {
        if (IsDragging) UpdatePointer(pointerPosition);
    }

    public bool TryFinishDrag(Vector2 screenPosition, out string error)
    {
        error = null;
        if (!IsDragging) { error = "No card is being dragged."; return false; }
        UpdatePointer(screenPosition);
        if (!IsDragging || currentTile == null || !previewCanPlace)
        {
            error = "The card was not dropped on a valid tile.";
            CancelDrag();
            return false;
        }
        int cardId = selectedCardId;
        Vector2Int coordinate = currentTile.Coordinate;
        try
        {
            return session.TryCommitBuild(cardId, coordinate, out _, out error);
        }
        finally
        {
            CancelDrag();
        }
    }

    public void CancelDrag()
    {
        SetTarget(null, false);
        if (preview != null) DestroyGenerated(preview.gameObject);
        preview = null;
        selectedCardId = 0;
        selectedCard = null;
    }

    private void OnEnable() => SubscribeSession();
    private void OnDisable() { UnsubscribeSession(); CancelDrag(); }

    private void SubscribeSession()
    {
        if (subscribedSession == session) return;
        UnsubscribeSession();
        subscribedSession = session;
        if (subscribedSession != null) subscribedSession.OnStateRestoring += CancelDrag;
    }

    private void UnsubscribeSession()
    {
        if (subscribedSession != null) subscribedSession.OnStateRestoring -= CancelDrag;
        subscribedSession = null;
    }

    private void SetTarget(Tile tile, bool canPlace)
    {
        if (currentTile == tile && previewCanPlace == canPlace) return;
        if (currentTile != null) currentTile.SetHighlight(false);
        currentTile = tile;
        previewCanPlace = canPlace;
        if (currentTile != null) currentTile.SetHighlight(canPlace);
        OnPreviewChanged?.Invoke(currentTile, canPlace);
    }

    private static void DestroyGenerated(GameObject value)
    {
        value.SetActive(false);
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
