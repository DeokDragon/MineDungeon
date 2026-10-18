using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DungeonResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text roomText;
    [SerializeField] private TMP_Text reviveText;
    [SerializeField] private TMP_Text rewardSummary;
    [SerializeField] private Button retryButton;
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private float clearPresentationDuration = .7f;
    private bool resultPending;
    private DungeonRunManager run;
    public bool IsShowing { get; private set; }
    public event System.Action Shown;

    private void Start()
    {
        resultPanel.SetActive(false);
        retryButton.onClick.AddListener(Retry);
        run = DungeonRunManager.Instance;
        run.RunEnded += ShowResult;
        player.GetComponent<PlayerAttack>().RequireFreshAttackInput();
        if (run.IsRunEnded) ShowResult();
    }

    private void ShowResult()
    {
        if (resultPending || IsShowing) return;
        resultPending = true;
        pauseMenu.EnterResultState();
        rooms.StopRoomCombat();
        StoneCombatBurst.ClearNonFinale();
        if (!run.IsRunCleared && Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.ResetShake();
        player.StopAllCoroutines();
        player.GetComponent<PlayerAttack>().CancelAttack();
        player.GetComponent<PlayerAttack>().enabled = false;
        player.GetComponent<PlayerMovement>().enabled = false;
        player.GetComponent<PlayerPotion>().enabled = false;
        title.text = run.IsRunCleared ? "던전 클리어" : "도전 실패";
        title.color = run.IsRunCleared ? new Color(.95f, .78f, .35f) : new Color(1f, .45f, .4f);
        roomText.text = $"최종 도달 방: {rooms.CurrentRoomNumber} / {rooms.TotalRoomCount}";
        reviveText.text = $"사용한 부활 횟수: {run.MaxRevives - run.RemainingRevives} / {run.MaxRevives}";
        var rewards=run.GetComponent<RewardInventorySession>();
        if(rewardSummary!=null)rewardSummary.text=rewards!=null
            ? $"이번 도전 획득 돈: {rewards.RunMoney:N0}\n처치 보상 경험치: {rewards.RunRewardExperience:N0}\n<size=75%>이미 지급된 보상 · MAX 이후 경험치 포함</size>" : "";
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (run.IsRunCleared) StartCoroutine(RevealClear());
        else RevealPanel();
    }

    private IEnumerator RevealClear()
    {
        float revealAt = Time.realtimeSinceStartup + clearPresentationDuration;
        while (Time.realtimeSinceStartup < revealAt) yield return null;
        RevealPanel();
    }

    private void RevealPanel()
    {
        IsShowing = true;
        Shown?.Invoke();
        resultPanel.transform.SetAsLastSibling();
        resultPanel.SetActive(true);
        retryButton.interactable = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    public void Retry()
    {
        if (!IsShowing || !retryButton.interactable) return;
        if (run.RestartCurrentScene()) retryButton.interactable = false;
    }

    private void OnDisable() { StopAllCoroutines(); resultPending = false; }

    private void OnDestroy()
    {
        if (run != null) run.RunEnded -= ShowResult;
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
    }
}
