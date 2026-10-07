using UnityEngine;

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
    }

    public bool TryUseRevive()
    {
        if (IsRunFailed || remainingRevives <= 0)
            return false;

        remainingRevives--;

        Debug.Log(
            $"부활 사용 / 남은 횟수 {remainingRevives}/{maxRevives}"
        );

        return true;
    }

    public void FailRun()
    {
        if (IsRunFailed)
            return;

        IsRunFailed = true;
        Debug.Log("도전 최종 실패: 부활 횟수를 모두 소모했습니다.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    public bool TryUsePotion()
    {
        if (IsRunFailed || remainingPotions <= 0)
            return false;

        remainingPotions--;
        return true;
    }
}