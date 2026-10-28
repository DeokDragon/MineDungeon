using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Read-only overlay on the existing HUD Canvas. It never consumes combat input.
[DefaultExecutionOrder(20000)]
[DisallowMultipleComponent]
public sealed class CurrentRoomMapUI : MonoBehaviour
{
    [SerializeField] DungeonRoomManager rooms;
    [SerializeField] PlayerHealth player;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] DungeonResultUI resultUI;
    [SerializeField] DungeonFloorData supportedFloor;
    [SerializeField] TMP_FontAsset font;
    RectTransform panel;
    CurrentRoomMapGraphic map;
    DungeonRoom observedRoom;
    StoneGolemBossAI boss;
    bool armed;
    public bool IsVisible => panel != null && panel.gameObject.activeSelf;

    void Awake()
    {
        panel = Rect("CurrentRoomMap", transform, new Vector2(560, 400), Vector2.zero);
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false; group.interactable = false;
        var background = panel.gameObject.AddComponent<CurrentRoomMapBackground>();
        background.sprite = Resources.Load<Sprite>("TabMapUI_v1/Background/TabMap_Background_560x400_A80");
        // The authored background already contains alpha 204 (80%).
        background.color = background.sprite ? Color.white : new Color(.035f, .045f, .055f, .8f);
        background.raycastTarget = false;
        Label("현재 방  ·  TAB", new Vector2(0, 174), 21, Color.white);
        Label("▲ 플레이어   ◆ 적/보스   ■ 출구: 잠김 / 개방", new Vector2(0, -176), 15, new Color(.85f,.87f,.89f));
        var area = Rect("TerrainAndMarkers", panel, new Vector2(512, 310), Vector2.zero);
        map = area.gameObject.AddComponent<CurrentRoomMapGraphic>();
        map.raycastTarget = false;
        var frame = Rect("MapFrame", panel, new Vector2(560, 400), Vector2.zero).gameObject.AddComponent<Image>();
        frame.sprite = Resources.Load<Sprite>("TabMapUI_v1/Frame/TabMap_Frame_560x400_v1");
        frame.type = Image.Type.Sliced; frame.fillCenter = false; frame.raycastTarget = false;
        frame.enabled = frame.sprite != null;
        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>()) label.transform.SetAsLastSibling();
        panel.gameObject.SetActive(false);
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    void Label(string text, Vector2 position, float size, Color color)
    {
        var label = Rect("Label", panel, new Vector2(536, 28), position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center; label.color = color; label.raycastTarget = false;
    }
    void OnEnable()
    {
        if (player) player.Died += OnPlayerDied;
        Canvas.willRenderCanvases += BeforeRender;
        armed = false;
    }
    void OnDisable()
    {
        if (player) player.Died -= OnPlayerDied;
        Canvas.willRenderCanvases -= BeforeRender;
        ObserveBoss(null); HideAndRequireRelease();
    }
    void OnDestroy() { if (panel) Destroy(panel.gameObject); }
    void OnPlayerDied(PlayerHealth _) => HideAndRequireRelease();
    void OnPresentation(StoneGolemBossAI _, bool transformation) => HideAndRequireRelease();
    void OnApplicationFocus(bool focused) { if (!focused) HideAndRequireRelease(); }
    void ObserveBoss(StoneGolemBossAI next)
    {
        if (boss == next) return;
        if (boss) boss.PresentationStarted -= OnPresentation;
        boss = next;
        if (boss) boss.PresentationStarted += OnPresentation;
    }
    bool Blocked()
    {
        var run = DungeonRunManager.Instance;
        if (!rooms || !player || !run || !supportedFloor || !rooms.CurrentFloor ||
            rooms.CurrentFloor.id != supportedFloor.id || rooms.CurrentDifficulty != 0 ||
            rooms.IsPreparing || rooms.IsTransitioning || !rooms.CurrentRoom ||
            player.IsDead || player.CombatLocked || run.IsRestarting || run.IsEnding || run.IsRunFailed ||
            !player.GetComponent<PlayerMovement>().enabled || Time.timeScale <= 0 ||
            InventoryUI.BlockCombatInput || MineEntranceUI.IsModalOpen ||
            (pauseMenu && pauseMenu.IsPaused) || (resultUI && resultUI.IsShowing) ||
            (boss && boss.IsPresenting)) return true;
        // The ended-clear state is intentionally allowed after the result window closes.
        return !rooms.CurrentRoom.GetComponent<RoomWalkableLayout>();
    }
    void HideAndRequireRelease()
    {
        armed = false;
        if (panel) panel.gameObject.SetActive(false);
    }
    void LateUpdate()
    {
        var current = rooms ? rooms.CurrentRoom : null;
        if (current != observedRoom)
        {
            observedRoom = current; HideAndRequireRelease();
            ObserveBoss(current ? current.GetComponentInChildren<StoneGolemBossAI>(true) : null);
            map.Bind(current, player);
        }
        bool held = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        UpdateVisibility(held, Blocked());
        if (IsVisible) map.SetVerticesDirty();
    }
    void UpdateVisibility(bool held, bool blocked)
    {
        if (blocked) { HideAndRequireRelease(); return; }
        if (!held) { panel.gameObject.SetActive(false); armed = true; return; }
        if (!armed) return;
        panel.gameObject.SetActive(true);
    }
    void BeforeRender()
    {
        // Catch UI/state changes made later in the frame, before any overlay is drawn.
        if (IsVisible && (Blocked() || rooms.CurrentRoom != observedRoom)) HideAndRequireRelease();
    }
}
