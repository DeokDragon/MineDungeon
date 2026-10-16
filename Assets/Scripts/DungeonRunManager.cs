using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DungeonRunManager : MonoBehaviour
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
        remainingRevives = maxRevives;
        remainingPotions = maxPotions;
        IsRunFailed = false;
        IsRunCleared = false;
    }

    public bool TryUseRevive()
    {
        if (IsRunEnded || IsRestarting || remainingRevives <= 0)
            return false;

        remainingRevives--;

        Debug.Log(
            $"부활 사용 / 남은 횟수 {remainingRevives}/{maxRevives}"
        );

        return true;
    }

    public void FailRun()
    {
        if (IsRunEnded || IsRestarting)
            return;

        IsRunFailed = true;
        RunEnded?.Invoke();
        Debug.Log("도전 최종 실패: 부활 횟수를 모두 소모했습니다.");
    }

    public void CompleteRun()
    {
        if (IsRunEnded || IsRestarting) return;
        IsRunCleared = true;
        RunEnded?.Invoke();
    }

    public bool RestartCurrentScene()
    {
        if (!IsRunEnded || IsRestarting) return false;
        string path = SceneManager.GetActiveScene().path;
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
        return true;
    }
}