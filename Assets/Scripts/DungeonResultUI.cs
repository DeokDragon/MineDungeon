using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DungeonResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text roomText;
    [SerializeField] private TMP_Text reviveText;
    [SerializeField] private Button retryButton;
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private PauseMenu pauseMenu;
    private DungeonRunManager run;
    public bool IsShowing { get; private set; }

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
        if (IsShowing) return;
        IsShowing = true;
        pauseMenu.EnterResultState();
        rooms.StopRoomCombat();
        player.StopAllCoroutines();
        player.GetComponent<PlayerAttack>().CancelAttack();
        player.GetComponent<PlayerAttack>().enabled = false;
        player.GetComponent<PlayerMovement>().enabled = false;
        player.GetComponent<PlayerPotion>().enabled = false;
        title.text = run.IsRunCleared ? "던전 클리어" : "도전 실패";
        title.color = run.IsRunCleared ? new Color(.95f, .78f, .35f) : new Color(1f, .45f, .4f);
        roomText.text = $"최종 도달 방: {rooms.CurrentRoomNumber} / {rooms.TotalRoomCount}";
        reviveText.text = $"사용한 부활 횟수: {run.MaxRevives - run.RemainingRevives} / {run.MaxRevives}";
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

    private void OnDestroy()
    {
        if (run != null) run.RunEnded -= ShowResult;
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
    }
}
