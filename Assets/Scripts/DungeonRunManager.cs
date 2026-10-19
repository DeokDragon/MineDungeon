using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public partial class DungeonRunManager : MonoBehaviour
{
    public static DungeonRunManager Instance { get; private set; }

    [SerializeField] private int maxRevives = 2;
    [SerializeField] private int remainingRevives;
    [Header("물약")]
    [SerializeField] private int maxPotions = 3;
    [SerializeField] private int remainingPotions;

    public int RemainingPotions => remainingPotions;
    public int MaxPotions => maxPotions;

    public int RemainingRevives => remainingRevives;
    public int MaxRevives => maxRevives;
    public int RemainingLives => IsRunFailed ? 0 : Mathf.Max(0, remainingRevives) + 1;
    public event System.Action ResourcesChanged;
    public bool IsRunFailed { get; private set; }
    public bool IsRunCleared { get; private set; }
    public bool IsRunEnded => IsRunFailed || IsRunCleared;
    public bool IsRestarting { get; private set; }
    public event System.Action RunEnded;

    private void Awake()
    {
        // 방 이동으로 관리자가 중복 생성되면 기존 것을 유지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 현재 테스트 씬의 첫 실행을 새 도전으로 취급
        BeginNewRun();
    }

    public void BeginNewRun()
    {
        Result=null; IsTracking=false; pendingClear=pendingFailure=settling=reviveInFlight=false; completedRevives=0;
        remainingRevives = maxRevives;
        remainingPotions = maxPotions;
        IsRunFailed = false;
        IsRunCleared = false;
        ResourcesChanged?.Invoke();
    }

    public bool TryUseRevive()
    {
        if (IsRunEnded || IsRestarting || remainingRevives <= 0)
            return false;

        remainingRevives--;
        reviveInFlight=true;
        ResourcesChanged?.Invoke();

        Debug.Log(
            $"부활 사용 / 남은 횟수 {remainingRevives}/{maxRevives}"
        );

        return true;
    }

    public void FailRun()
    {
        if (IsRunEnded || IsRestarting)
            return;

        pendingFailure=true;
    }

    public void CompleteRun()
    {
        if(IsRunEnded||IsRestarting)return;
        pendingClear=true;
    }

    public bool RestartCurrentScene()
    {
        if (!IsRunEnded || IsRestarting) return false;
        return RetryDungeon();
    }

    public bool ReturnToMain()
    {
        if (IsRestarting) return false;
        if (CharacterSaveService.Current != null && !CharacterSaveService.Current.Flush()) return false;
        string path = "Assets/Scenes/Main.unity";
        if (!Application.CanStreamedLevelBeLoaded(path))
        {
            Debug.LogError("The dungeon scene must be enabled in Build Settings.", this);
            return false;
        }
        IsRestarting = true; // Guard survives destruction of the old Canvas.
        StartCoroutine(ReloadRun(path));
        return true;
    }

    private IEnumerator ReloadRun(string path)
    {
        // The singleton survives reload; reset before new scene Start methods.
        BeginNewRun();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        IsRestarting = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    public bool TryUsePotion()
    {
        if (IsRunEnded || IsRestarting || remainingPotions <= 0)
            return false;

        remainingPotions--;
        ResourcesChanged?.Invoke();
        return true;
    }
}
