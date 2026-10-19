using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-independent prefab references, shared by clear and failure.
public sealed class DungeonResultView : MonoBehaviour
{
    public GameObject overlay, reopen;
    public RectTransform content, titleRoot, rewardsRoot, recordsRoot, itemContent;
    public CanvasGroup overlayGroup, rewardsGroup, recordsGroup;
    public TMP_Text eyebrow, title, subtitle, floor, failureNote, itemCount, empty, gold, experience, level, status;
    public TMP_Text[] statistics;
    public TMP_Text timeLabel;
    public Image experienceFill;
    public Button closeButton, reopenButton, retryButton, lobbyButton, saveRetryButton;
    public ScrollRect scroll;
    public ResultItemCard cardTemplate;
}
