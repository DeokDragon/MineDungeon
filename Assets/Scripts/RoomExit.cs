using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class RoomExit : MonoBehaviour
{
    [SerializeField] private Color lockedColor = new Color(0.65f, 0.2f, 0.15f);
    [SerializeField] private Color openColor = new Color(0.2f, 0.9f, 0.4f);

    private BoxCollider2D exitCollider;
    private SpriteRenderer exitRenderer;
    private bool hasEntered;
    private DungeonRoom owner;
    private DungeonRoomManager roomManager;

    public void Bind(DungeonRoom room, DungeonRoomManager manager)
    {
        owner = room;
        roomManager = manager;
    }

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        // RoomController can set the state before this component's Awake.
        ApplyState();
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (!open)
            hasEntered = false;
        ApplyState();
    }

    private void ApplyState()
    {
        if (exitCollider == null)
            exitCollider = GetComponent<BoxCollider2D>();
        if (exitRenderer == null)
            exitRenderer = GetComponent<SpriteRenderer>();

        exitCollider.isTrigger = IsOpen;
        exitRenderer.color = IsOpen ? openColor : lockedColor;
        // An open exit must no longer obstruct combat's Wall linecasts.
        gameObject.layer = LayerMask.NameToLayer(IsOpen ? "Default" : "Wall");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryEnter(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also handles an exit opening while the player is touching it.
        TryEnter(other);
    }

    private void TryEnter(Collider2D other)
    {
        if (!IsOpen || hasEntered || Time.timeScale == 0f)
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
            return;

        DungeonRunManager run = DungeonRunManager.Instance;
        if (run != null && run.IsRunFailed)
            return;

        if (roomManager != null)
        {
            roomManager.RequestNextRoom(owner, player);
            return;
        }

        hasEntered = true;
        Debug.Log("Exit reached! Room complete. Next room is not connected yet.", this);
    }
}
