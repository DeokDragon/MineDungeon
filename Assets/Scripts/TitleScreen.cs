using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Scene-local presentation only. The existing Main scene owns all save/gameplay initialization.
public sealed class TitleScreen : MonoBehaviour
{
    [Serializable] private sealed class Lantern
    {
        public Image flame;
        public Sprite[] frames;
        public Image[] lights;
        public float frameSeconds, frameOffset, lightSeconds, lightPhase;

        public void Animate(double elapsed)
        {
            int frame = (int)(Math.Floor((elapsed + frameOffset) / frameSeconds) % frames.Length);
            flame.sprite = frames[frame];
            float brightness = .94f + .06f * (float)Math.Sin(elapsed * (2 * Math.PI / lightSeconds) + lightPhase);
            // The source art uses straight alpha. Keep flame alpha intact and couple its RGB to the light.
            flame.color = new Color(brightness, brightness, brightness, 1);
            foreach (var light in lights) light.color = new Color(1, 1, 1, brightness);
        }
    }

    [SerializeField] private RectTransform viewport, composition;
    [SerializeField] private Image slime;
    [SerializeField] private Sprite[] slimeFrames;
    [SerializeField] private Lantern[] lanterns;
    [SerializeField] private Button startButton, settingsButton, quitButton, confirmQuitButton, cancelQuitButton;
    [SerializeField] private GameObject quitOverlay;
    [SerializeField] private TMP_Text quitMessage;
    [SerializeField] private string lobbyScene = "Assets/Scenes/Main.unity";
    [SerializeField, Min(.01f)] private float slimeFrameSeconds = .24f;
    private double animationStarted;
    private bool starting, confirmingQuit;

    private void OnEnable() { animationStarted = Time.unscaledTimeAsDouble; }
    private void Start()
    {
        startButton.onClick.AddListener(BeginGame);
        quitButton.onClick.AddListener(ShowQuitConfirmation);
        confirmQuitButton.onClick.AddListener(ConfirmQuit);
        cancelQuitButton.onClick.AddListener(CancelQuit);
        // PauseMenu settings require the Main scene's save service and camera. No new settings are created here.
        settingsButton.interactable = false;
        quitOverlay.SetActive(false);
        Select(startButton);
        FitComposition();
    }
    private void Update()
    {
        FitComposition();
        Animate(Time.unscaledTimeAsDouble - animationStarted);
        if (confirmingQuit && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CancelQuit();
    }
    private void FitComposition()
    {
        // Fit the complete reference composition, including its UI and character, at every aspect ratio.
        float scale = Mathf.Min(viewport.rect.width / 1920f, viewport.rect.height / 1080f);
        composition.localScale = new Vector3(scale, scale, 1);
    }
    private void Animate(double elapsed)
    {
        slime.sprite = slimeFrames[(int)(Math.Floor(elapsed / slimeFrameSeconds) % slimeFrames.Length)];
        foreach (var lantern in lanterns) lantern.Animate(elapsed);
    }
    private static void Select(Button button)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    public void BeginGame()
    {
        if (starting || confirmingQuit) return;
        starting = true;
        startButton.interactable = quitButton.interactable = false;
        // Loading Main follows its existing first-run/load path. No save service exists in this scene.
        SceneManager.LoadSceneAsync(lobbyScene, LoadSceneMode.Single);
    }
    public void ShowQuitConfirmation()
    {
        if (starting || confirmingQuit) return;
        confirmingQuit = true;
        startButton.interactable = quitButton.interactable = false;
        quitMessage.text = "게임을 종료할까요?";
        quitOverlay.SetActive(true);
        Select(cancelQuitButton);
    }
    public void CancelQuit()
    {
        if (starting || !confirmingQuit) return;
        confirmingQuit = false;
        quitOverlay.SetActive(false);
        startButton.interactable = quitButton.interactable = true;
        Select(quitButton);
    }
    public void ConfirmQuit()
    {
        if (starting || !confirmingQuit) return;
        starting = true;
        // Nothing has loaded or changed persistent character data while on the title screen.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
