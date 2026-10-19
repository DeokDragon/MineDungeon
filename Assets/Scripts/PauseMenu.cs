using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-1000)]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel, menuPage, controlsPage, quitPage;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Button resumeButton, settingsButton, returnButton, quitButton, confirmButton, cancelButton, settingsBackButton;
    [SerializeField] private TMP_Text confirmationText, errorText;
    [SerializeField] private Slider shakeSlider;
    private bool isPaused, returning, busy;
    public bool IsPaused => isPaused;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool ResultLocked => DungeonRunManager.Instance != null && (DungeonRunManager.Instance.IsRunEnded || DungeonRunManager.Instance.IsRestarting);
    private bool InDungeon => FindFirstObjectByType<DungeonRoomManager>() is DungeonRoomManager rooms && !rooms.IsPreparing;
    private void Start()
    {
        pausePanel.SetActive(false);
        resumeButton.onClick.AddListener(Resume); settingsButton.onClick.AddListener(ShowControls);
        returnButton.onClick.AddListener(ShowReturnConfirmation); quitButton.onClick.AddListener(ShowQuitConfirmation);
        confirmButton.onClick.AddListener(ConfirmQuit); cancelButton.onClick.AddListener(ShowMainMenu); settingsBackButton.onClick.AddListener(ShowMainMenu);
        shakeSlider.SetValueWithoutNotify(CharacterSaveService.Current != null ? CharacterSaveService.Current.ShakeMultiplier : 1);
        shakeSlider.onValueChanged.AddListener(value => { CharacterSaveService.Current?.SetShakeMultiplier(value); FindFirstObjectByType<CameraFollow>()?.SetShakeSetting(value); });
    }
    private void Update()
    {
        if (ResultLocked || InventoryUI.BlockPauseInput || busy) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        HandleEscape();
    }
    internal void HandleEscape()
    {
        if (ResultLocked || InventoryUI.BlockPauseInput || busy) return;
        if (!isPaused) Pause();
        else if (controlsPage.activeSelf || quitPage.activeSelf) ShowMainMenu();
        else Resume();
    }
    public void Pause()
    {
        if (isPaused || ResultLocked || InventoryUI.BlockPauseInput) return;
        previousTimeScale = Time.timeScale; previousAudioPause = AudioListener.pause;
        isPaused = true; Time.timeScale = 0; AudioListener.pause = true;
        pausePanel.SetActive(true); pausePanel.transform.SetAsLastSibling(); ShowMainMenu();
    }
    public void Resume()
    {
        if (!isPaused || ResultLocked || busy) return;
        playerAttack?.RequireFreshAttackInput(); RestoreTime(); pausePanel.SetActive(false);
    }
    public void ShowMainMenu()
    {
        if (busy) return;
        menuPage.SetActive(true); controlsPage.SetActive(false); quitPage.SetActive(false);
        bool inDungeon = InDungeon; returnButton.gameObject.SetActive(inDungeon);
        var quitRect = (RectTransform)quitButton.transform; float y = inDungeon ? .13f : .30f;
        quitRect.anchorMin = new Vector2(.12f, y); quitRect.anchorMax = new Vector2(.88f, y + .12f);
        errorText.text = "";
    }
    public void ShowControls()
    {
        if (busy) return;
        menuPage.SetActive(false); controlsPage.SetActive(true); quitPage.SetActive(false);
    }
    public void ShowReturnConfirmation()
    {
        if (!InDungeon || busy) return; returning = true; ShowConfirmation();
    }
    public void ShowQuitConfirmation() { if (busy) return; returning = false; ShowConfirmation(); }
    private void ShowConfirmation()
    {
        menuPage.SetActive(false); controlsPage.SetActive(false); quitPage.SetActive(true); errorText.text = "";
        confirmationText.text = returning ? "진행 중인 던전을 끝내고 광산 밖으로 귀환할까요?\n\n이미 받은 돈·경험치·재료는 보존됩니다. 미획득 상자 보상은 지급되지 않습니다." :
            "게임을 종료할까요?\n\n성장과 보유 아이템을 저장합니다." + (InDungeon ? "\n던전 진행은 저장하지 않으며 다음 실행은 메인에서 시작합니다." : "");
    }
    public void ConfirmQuit()
    {
        if (busy || !isPaused || !quitPage.activeSelf) return;
        busy = true;
        var save = CharacterSaveService.Current;
        if (save == null || !save.Flush()) { errorText.text = save != null ? save.Status : "저장 연결을 확인해 주세요."; busy = false; return; }
        if (returning)
        {
            if (!DungeonRunManager.Instance.ReturnToMain()) { errorText.text = "귀환 실패: 재시도하거나 취소해 주세요."; busy = false; }
            return;
        }
#if UNITY_EDITOR
        // Stop Play Mode, never close the Unity Editor.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void RestoreTime()
    {
        if (!isPaused || ResultLocked) return;
        Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; isPaused = false;
    }
    public void EnterResultState() { isPaused = false; pausePanel.SetActive(false); }
    private void OnDisable() { RestoreTime(); }
}
