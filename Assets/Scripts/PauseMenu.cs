using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject menuPage;
    [SerializeField] private GameObject controlsPage;
    [SerializeField] private GameObject quitPage;
    [SerializeField] private PlayerAttack playerAttack;

    private bool isPaused;
    private float previousTimeScale;
    private bool previousAudioPause;

    private void Start()
    {
        pausePanel.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (!isPaused)
        {
            Pause();
        }
        else if (controlsPage.activeSelf || quitPage.activeSelf)
        {
            ShowMainMenu();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        if (isPaused)
            return;

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;

        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        pausePanel.SetActive(true);
        ShowMainMenu();
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        // メニュー操作後に、新しいクリックを要求する
        if (playerAttack != null)
            playerAttack.RequireFreshAttackInput();

        RestoreTime();
        pausePanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        menuPage.SetActive(true);
        controlsPage.SetActive(false);
        quitPage.SetActive(false);
    }

    public void ShowControls()
    {
        menuPage.SetActive(false);
        controlsPage.SetActive(true);
        quitPage.SetActive(false);
    }

    public void ShowQuitConfirmation()
    {
        menuPage.SetActive(false);
        controlsPage.SetActive(false);
        quitPage.SetActive(true);
    }

    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        Debug.Log("종료 버튼 정상 작동: 빌드한 게임에서는 종료됩니다.");
#else
        RestoreTime();
        Application.Quit();
#endif
    }

    private void RestoreTime()
    {
        if (!isPaused)
            return;

        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        isPaused = false;
    }

    private void OnDisable()
    {
        RestoreTime();
    }
}