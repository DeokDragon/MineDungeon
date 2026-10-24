using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-1100)]
public sealed class InventoryUI : MonoBehaviour
{
    [System.Serializable] private sealed class Tab { public Button button; public Image background, border; }
    [System.Serializable] private sealed class Slot { public Button button; public TMP_Text name, marker; public Image icon, highlight; }
    [System.Serializable] private sealed class StatCard
    {
        public TMP_Text points, amount, arrow;
        public Button invest, header;
        public GameObject details;
        public LayoutElement layout;
        public Image border;
        public Color accent;
    }
    [System.Serializable] private sealed class PassiveCard { public TMP_Text state; public Image background; }
    [SerializeField] private GameObject characterPage, statPage, statNotification;
    [SerializeField] private Button characterTab, statTab;
    [SerializeField] private Image characterTabBorder, statTabBorder;
    [SerializeField] private Button[] multiplierButtons;
    [SerializeField] private TMP_Text unspentText, investmentRule, levelText;
    [SerializeField] private StatCard[] statCards;
    [SerializeField] private PassiveCard[] passiveCards;
    [SerializeField] private ScrollRect growthScroll;
    [SerializeField] private Button resetStatsButton, resetCancelButton, resetConfirmButton;
    [SerializeField] private GameObject resetDefault, resetConfirmation;
    [SerializeField] private TMP_Text resetGoldText, resetSummary;
    private bool resetConfirming, resetBusy;
    private string resetReviewedSnapshot;
    private Button[] feedbackButtons;
    private Image[] feedbackBorders;
    private TMP_Text[] feedbackLabels;
    private int hoveredButton = -1, pressedButton = -1;
    private PlayerProgression growth;
    public const float CollapsedStatCardHeight = 54f, ExpandedStatCardHeight = 212f;
    private int expandedStat = -1, hoveredStat = -1;
    private int investmentMultiplier = 1;
    private string investmentError;
    [SerializeField] private PlayerSkills player;
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton, clearSearchButton;
    [SerializeField] private TMP_InputField search;
    [SerializeField] private TMP_Dropdown sorting;
    [SerializeField] private TMP_Text listTitle, countText, emptyText, statsText, restrictionText, noticeText;
    [SerializeField] private Tab[] tabs;
    [SerializeField] private Slot eSlot, rSlot;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private InventorySkillCard cardPrefab;
    [SerializeField] private RectTransform tooltip, canvasRect;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField, Min(0)] private float hoverDelay = .2f;
    private static InventoryUI active;
    private SkillInventorySession session;
    private RewardInventorySession rewards;
    private DungeonRunManager run;
    private PlayerHealth health;
    private PlayerAttack attack;
    private PlayerMovement movement;
    private PlayerSkillAttackPower powers;
    private InventoryCategory category;
    private readonly List<InventorySort> sortChoices = new List<InventorySort>();
    private readonly List<InventorySkillCard> cards = new List<InventorySkillCard>();
    private bool waitingRelease, externalTooltip;
    private float previousStatsPhysical, previousStatsMagic;
    private int externalOpenFrame=-1;
    private int suppressedFrame = -1;
    private float noticeUntil;
    private int lastOwnedCount = -1;
    private float lastGridWidth = -1;
    private RectTransform hoverTarget;
    private SkillData hoverSkill;
    private MaterialData hoverMaterial;
    private int hoverSlot = -1;
    private string hoverNote;
    private float hoverAt;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private float equipPulseStarted;
    private Color eHighlightColor, rHighlightColor;
    public PlayerSkills Player => player;
    public string SelectedId { get; private set; }
    public string SelectedMaterialId { get; private set; }
    public long MaterialQuantity(string id) => rewards != null ? rewards.Quantity(id) : 0;
    public bool IsOpen { get; private set; }
    public bool CanEquip => rooms != null && rooms.IsPreparing && !rooms.IsTransitioning && run != null && !run.IsRunEnded && !run.IsRestarting;
    public static bool BlockCombatInput => MineEntranceUI.IsModalOpen || (active != null && (active.IsOpen || active.waitingRelease || active.suppressedFrame == Time.frameCount));
    public static bool BlockPauseInput => MineEntranceUI.IsModalOpen || (active != null && (active.IsOpen || active.suppressedFrame == Time.frameCount));

    private void Awake()
    {
        active = this;
        eHighlightColor = eSlot.highlight.color; rHighlightColor = rSlot.highlight.color;
        health = player.GetComponent<PlayerHealth>(); attack = player.GetComponent<PlayerAttack>();
        movement = player.GetComponent<PlayerMovement>(); powers = player.GetComponent<PlayerSkillAttackPower>();
        panel.SetActive(false); tooltip.gameObject.SetActive(false);
    }
    private void Start()
    {
        session = SkillInventorySession.Instance; run = DungeonRunManager.Instance;
        growth = run.GetComponent<PlayerProgression>();
        if (growth != null) growth.Changed += RefreshGrowth;
        rewards=run.GetComponent<RewardInventorySession>();if(rewards!=null)rewards.Changed+=RewardsChanged;
        session.Changed += RefreshAll; run.RunEnded += OnResult; health.Died += OnPlayerDied;
        closeButton.onClick.AddListener(Close); clearSearchButton.onClick.AddListener(ClearSearch);
        search.onValueChanged.AddListener(SearchChanged); sorting.onValueChanged.AddListener(SortChanged); scroll.onValueChanged.AddListener(Scrolled);
        eSlot.button.onClick.AddListener(EquipE); rSlot.button.onClick.AddListener(EquipR);
        for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].button.onClick.AddListener(() => SetCategory(index)); }
        if (characterTab != null)
        {
            characterTab.onClick.AddListener(() => SetCharacterTab(0)); statTab.onClick.AddListener(() => SetCharacterTab(1));
            for (int i = 0; i < multiplierButtons.Length; i++) { int value = i == 0 ? 1 : i == 1 ? 5 : 10; multiplierButtons[i].onClick.AddListener(() => SetInvestmentMultiplier(value)); }
            for (int i = 0; i < statCards.Length; i++)
            {
                int index = i;
                statCards[i].invest.onClick.AddListener(() => InvestStat(index));
                if (statCards[i].header != null) statCards[i].header.onClick.AddListener(() => ToggleStatCard(index));
            }
            if (resetStatsButton != null)
            {
                resetStatsButton.onClick.AddListener(BeginStatReset);
                resetCancelButton.onClick.AddListener(CancelStatReset);
                resetConfirmButton.onClick.AddListener(ConfirmStatReset);
            }
            InitializeGrowthButtonFeedback();
            ApplyAccordion();
            SetCharacterTab(0);
            growthScroll.onValueChanged.AddListener(Scrolled);
        }
        RefreshGrowth();
        SetCategory(0);
    }
    private void Update()
    {
        if (run == null) return;
        if (IsOpen && (run.IsRunEnded || run.IsRestarting || health.IsDead)) CloseInternal();
        if (waitingRelease && !CombatKeyHeld()) waitingRelease = false;
        var keys = Keyboard.current;
        if (keys != null)
        {
            if (IsOpen && keys.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (keys.iKey.wasPressedThisFrame && externalOpenFrame!=Time.frameCount && !(IsOpen && search.isFocused)) { if (IsOpen) Close(); else TryOpen(); }
        }
        if (noticeUntil > 0 && Time.unscaledTime >= noticeUntil) { noticeText.text = ""; noticeUntil = 0; }
        if (!IsOpen) { if(externalTooltip&&hoverTarget!=null&&Time.unscaledTime>=hoverAt)ShowTooltip();return; }
        if (SelectedId != null && !CanEquip) CancelPendingEquip();
        if (SelectedId != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandlePendingPointer(Mouse.current.position.ReadValue());
        UpdateEquipPulse();
        if (!Mathf.Approximately(lastGridWidth, ((RectTransform)grid.transform).rect.width)) UpdateGrid();
        RefreshStats();
        RefreshGrowth();
        if (!Mathf.Approximately(previousStatsPhysical, powers.PhysicalAttack) || !Mathf.Approximately(previousStatsMagic, powers.MagicAttack))
        {
            previousStatsPhysical = powers.PhysicalAttack; previousStatsMagic = powers.MagicAttack;
            if (CurrentSort == InventorySort.Damage) RefreshList();
        }
        if (hoverTarget != null && Time.unscaledTime >= hoverAt) ShowTooltip();
    }
    private static bool CombatKeyHeld()
    {
        var k = Keyboard.current;
        return (Mouse.current != null && Mouse.current.leftButton.isPressed) || (k != null && (k.eKey.isPressed || k.rKey.isPressed || k.qKey.isPressed || k.spaceKey.isPressed));
    }
    private void SuppressClosingInput()
    {
        suppressedFrame = Time.frameCount; waitingRelease = true; if (attack != null) attack.RequireFreshAttackInput();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    public void SuppressAfterModal() { SuppressClosingInput(); }
    public bool OpenFromExternal() { externalOpenFrame=Time.frameCount;return TryOpen(); }
    public bool TryOpen()
    {
        if (IsOpen) return true;
        if (MineEntranceUI.IsModalOpen || run == null || run.IsRunEnded || run.IsRestarting || health.IsDead || rooms.IsTransitioning || pauseMenu.IsPaused) return false;
        if (player.IsCasting || movement.IsDodging) { Notice("스킬 시전·회피 중에는 열 수 없습니다"); return false; }
        IsOpen = true;
        panel.SetActive(true); panel.transform.SetAsLastSibling(); tooltip.SetAsLastSibling();
        attack.RequireFreshAttackInput(); RefreshAll(); return true;
    }
    public void Close() { CloseInternal(); }
    private void CloseInternal()
    {
        if (!IsOpen) return;
        CancelPendingEquip();
        CancelStatReset(); HideTooltip(); sorting.Hide(); search.DeactivateInputField(); panel.SetActive(false); IsOpen = false;
        SuppressClosingInput();
    }
    public void StartDungeon()
    {
        if (!CanEquip || !rooms.StartDungeon()) return;
        CloseInternal();
        SuppressClosingInput();
    }
    private void OnResult() { CloseInternal(); HideTooltip(); }
    private void OnPlayerDied(PlayerHealth _) { CloseInternal(); }
    public void Select(string id)
    {
        if (!IsOpen || session.Find(id) == null) return;
        CancelPendingEquip(); SelectedMaterialId = null;
        if (CanEquip) { SelectedId = id; equipPulseStarted = Time.unscaledTime; }
        RefreshSelection(); UpdateEquipPulse();
    }
    public void SelectMaterial(string id){CancelPendingEquip();SelectedMaterialId=id;RefreshSelection();}
    private void CancelPendingEquip()
    {
        SelectedId = null;
        eSlot.highlight.color = eHighlightColor; rSlot.highlight.color = rHighlightColor;
        RefreshSelection();
    }
    private void HandlePendingPointer(Vector2 position)
    {
        // Inspect the top UI hit before EventSystem dispatches clicks. Never forward a cancel to world input.
        pointerHits.Clear();
        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, pointerHits);
        Transform hit = pointerHits.Count > 0 ? pointerHits[0].gameObject.transform : null;
        if (hit != null && (hit.GetComponentInParent<InventorySkillCard>() != null ||
            hit.IsChildOf(eSlot.button.transform) || hit.IsChildOf(rSlot.button.transform))) return;
        if (hit != null && hit.IsChildOf(panel.transform) && hit.GetComponentInParent<Selectable>() != null) return;
        CancelPendingEquip();
        suppressedFrame = Time.frameCount; waitingRelease = true;
        if (attack != null) attack.RequireFreshAttackInput();
    }
    private void UpdateEquipPulse()
    {
        if (SelectedId == null || !CanEquip) return;
        float alpha = .65f + .35f * Mathf.Cos((Time.unscaledTime - equipPulseStarted) * (2f * Mathf.PI / .8f));
        eSlot.highlight.color = new Color(eHighlightColor.r, eHighlightColor.g, eHighlightColor.b, eHighlightColor.a * alpha);
        rSlot.highlight.color = new Color(rHighlightColor.r, rHighlightColor.g, rHighlightColor.b, rHighlightColor.a * alpha);
    }
    public bool Equip(int slot)
    {
        if (!CanEquip || SelectedId == null) { Notice("준비 중에만 변경할 수 있습니다"); return false; }
        bool equipped = session.TryEquip(slot, SelectedId, rooms);
        if (equipped) CancelPendingEquip();
        return equipped;
    }
    private void EquipE() { Equip(0); }
    private void EquipR() { Equip(1); }
    private void Notice(string text) { noticeText.text = text; noticeUntil = Time.unscaledTime + 2.5f; }
    private void ClearSearch() { search.text = ""; }
    private void SearchChanged(string _) { RefreshList(); }
    private void SortChanged(int _) { RefreshList(); }
    private void Scrolled(Vector2 _) { HideTooltip(); }
    private InventorySort CurrentSort => sortChoices.Count > sorting.value ? sortChoices[sorting.value] : InventorySort.Recent;
    public void SetCategory(int index)
    {
        CancelPendingEquip();
        category = (InventoryCategory)Mathf.Clamp(index, 0, 6); HideTooltip();
        sortChoices.Clear(); sortChoices.AddRange(new[] { InventorySort.Recent, InventorySort.Name, InventorySort.Rarity });
        var labels = new List<string> { "최근 획득순", "이름순", "희귀도 높은순" };
        if (category == InventoryCategory.All) { sortChoices.Add(InventorySort.Category); labels.Add("아이템 분류순"); }
        if (category == InventoryCategory.Skill) { sortChoices.AddRange(new[] { InventorySort.Type, InventorySort.Cooldown, InventorySort.Damage }); labels.AddRange(new[] { "물리/마법 타입순", "쿨타임 짧은순", "예상 피해 높은순" }); }
        if(category==InventoryCategory.Material){sortChoices.Add(InventorySort.Quantity);labels.Add("보유 수량 많은순");}
        sorting.ClearOptions(); sorting.AddOptions(labels); sorting.SetValueWithoutNotify(0); sorting.RefreshShownValue();
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].background.color = i == (int)category ? new Color(.29f,.24f,.13f) : new Color(.09f,.12f,.16f);
            tabs[i].border.enabled = i == (int)category;
        }
        RefreshList();
    }
    private int OwnedCount => (session?.Owned.Count??0)+(rewards?.Materials.Count??0);
    private void RewardsChanged(){if(IsOpen)RefreshList();else lastOwnedCount=-1;}
    private void RefreshAll() { if (session != null && lastOwnedCount != OwnedCount) RefreshList(); RefreshSelection(); RefreshStats(); }
    private void RefreshList()
    {
        if (session == null) return;
        lastOwnedCount = OwnedCount;
        HideTooltip();
        foreach (var card in cards) { card.gameObject.SetActive(false); Destroy(card.gameObject); } cards.Clear();
        bool implemented = category == InventoryCategory.All || category == InventoryCategory.Skill || category == InventoryCategory.Material;
        var results = InventoryPresentation.QueryItems(session,rewards,category, search.text, CurrentSort, player);
        listTitle.text = category == InventoryCategory.All ? "전체 아이템" : category == InventoryCategory.Skill ? "보유 스킬" : InventoryPresentation.Categories[(int)category];
        if(category==InventoryCategory.Material)listTitle.text="보유 재료";
        countText.text = $"검색 결과 {results.Count}개  /  전체 보유 {OwnedCount}개";
        int categoryCount=category==InventoryCategory.Material?(rewards?.Materials.Count??0):category==InventoryCategory.Skill?session.Owned.Count:OwnedCount;
        emptyText.text = !implemented ? "준비 중" : categoryCount == 0 ? "보유 아이템이 없습니다" : results.Count == 0 ? "검색 결과가 없습니다" : "";
        foreach (var entry in results) { var card = Instantiate(cardPrefab, grid.transform); if(entry.Skill!=null)card.Bind(this, entry.Skill);else card.Bind(this,entry.Material); cards.Add(card); }
        UpdateGrid(); scroll.verticalNormalizedPosition = 1f; RefreshSelection();
    }
    private void RefreshSelection()
    {
        foreach (var card in cards) card.Refresh();
        RefreshSlot(eSlot, 0); RefreshSlot(rSlot, 1);
        restrictionText.text = CanEquip ? (SelectedId == null ? "" : "장착할 슬롯을 선택하세요 · 반대 슬롯이면 교환") : "준비 중에만 변경할 수 있습니다";
        if(SelectedMaterialId!=null)restrictionText.text="재료는 장착할 수 없습니다";
    }
    private void RefreshSlot(Slot slot, int index)
    {
        var skill = player.Equipped(index); slot.name.text = skill != null ? skill.displayName : "비어 있음";
        slot.marker.text = InventoryPresentation.Marker(skill); slot.icon.sprite = skill != null ? skill.icon : null;
        slot.icon.enabled = slot.icon.sprite != null; slot.marker.enabled = !slot.icon.enabled;
        slot.highlight.enabled = CanEquip && SelectedId != null;
        slot.button.interactable = CanEquip && SelectedId != null;
    }
    private void RefreshStats()
    {
        statsText.text = $"최종 물리 공격 스탯  <color=#FFCF75>{powers.PhysicalAttack:0.##}</color>\n최종 마법 공격 스탯  <color=#FFCF75>{powers.MagicAttack:0.##}</color>\n최대 HP                    {health.MaxHealth}\n방어력                      <color=#9AA6B4>미구현</color>\n피해 감소율               <color=#9AA6B4>미구현</color>\n공격 간격                  {attack.AttackInterval:0.00}초\n이동 속도                  {movement.FinalMoveSpeed:0.##}";
    }
    public void SetCharacterTab(int index)
    {
        if (characterPage == null) return;
        CancelPendingEquip();
        CancelStatReset();
        bool stats = index == 1;
        characterPage.SetActive(!stats); statPage.SetActive(stats); HideTooltip();
        hoveredStat = -1; RefreshStatBorders();
        RefreshGrowth();
    }
    public void ToggleStatCard(int index)
    {
        if (index < 0 || index >= statCards.Length || statCards[index].header == null) return;
        expandedStat = expandedStat == index ? -1 : index;
        HideTooltip(); ApplyAccordion();
    }
    public void SetStatCardHover(int index, bool hovered)
    {
        if (hovered) hoveredStat = index;
        else if (hoveredStat == index) hoveredStat = -1;
        RefreshStatBorders();
    }
    private void RefreshStatBorders()
    {
        for (int i = 0; i < statCards.Length; i++)
        {
            var card = statCards[i];
            if (card.border == null) continue;
            card.border.color = expandedStat == i
                ? (hoveredStat == i ? Color.Lerp(card.accent, Color.white, .2f) : card.accent)
                : (hoveredStat == i ? new Color(.65f,.72f,.77f) : new Color(.27f,.30f,.31f));
        }
    }
    private void ApplyAccordion()
    {
        for (int i = 0; i < statCards.Length; i++)
        {
            var card = statCards[i];
            if (card.details == null) continue;
            bool open = i == expandedStat;
            card.details.SetActive(open); card.layout.preferredHeight = open ? ExpandedStatCardHeight : CollapsedStatCardHeight;
            // Rotate the existing '>' glyph into a downward chevron, avoiding a new font/art asset.
            card.arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, open ? -90f : 0f);
        }
        RefreshStatBorders();
        if (growthScroll == null) return;
        growthScroll.StopMovement();
        LayoutRebuilder.ForceRebuildLayoutImmediate(growthScroll.content);
        if (expandedStat < 0) { growthScroll.verticalNormalizedPosition = 1f; return; }
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(growthScroll.viewport, statCards[expandedStat].layout.transform);
        float shift = bounds.min.y < growthScroll.viewport.rect.yMin ? growthScroll.viewport.rect.yMin - bounds.min.y
            : bounds.max.y > growthScroll.viewport.rect.yMax ? growthScroll.viewport.rect.yMax - bounds.max.y : 0f;
        growthScroll.content.anchoredPosition += Vector2.up * shift;
    }
    public void SetInvestmentMultiplier(int value)
    {
        if (value != 1 && value != 5 && value != 10) return;
        investmentMultiplier = value; RefreshGrowth();
    }
    public void InvestStat(int index)
    {
        if (!IsOpen || resetConfirming || resetBusy || growth == null || index < 0 || index > 2) return;
        int invested; string error;
        if (!growth.TryInvest((PlayerProgression.Stat)index, investmentMultiplier, out invested, out error))
        {
            investmentError = growth.CanInvest && growth.InvestablePoints((PlayerProgression.Stat)index, investmentMultiplier) > 0
                ? "저장 실패 · 투자 미적용 · 다시 시도해 주세요" : error;
        }
        else { investmentError = null; }
        RefreshGrowth(); RefreshStats();
    }
    private void RefreshGrowth()
    {
        if (growth == null || characterPage == null) return;
        statNotification.SetActive(growth.UnspentStatPoints > 0);
        unspentText.text = $"미사용  <color=#FFCF75>{growth.UnspentStatPoints} P</color>";
        levelText.text = $"Lv. {growth.Level}";
        RefreshStatReset();
        bool allowed = growth.CanInvest;
        string hint = GrowthButtonHint();
        investmentRule.text = !string.IsNullOrEmpty(hint) ? hint : !string.IsNullOrEmpty(investmentError) ? investmentError : !allowed ? "던전에서는 열람만 가능합니다 · 투자 불가"
            : "";
        investmentRule.color = !string.IsNullOrEmpty(hint) || !string.IsNullOrEmpty(investmentError) ? new Color(1,.65f,.4f) : new Color(.58f,.66f,.72f);
        for (int i = 0; i < statCards.Length; i++)
        {
            var stat = (PlayerProgression.Stat)i;
            int amount = growth.InvestablePoints(stat, investmentMultiplier);
            statCards[i].points.text = $"{growth.PointsIn(stat):00} <size=65%>/ 100P</size>";
            statCards[i].amount.text = amount > 0 ? $"+{amount}" : "+";
            statCards[i].invest.interactable = allowed && !resetConfirming && !resetBusy && amount > 0;
        }
        for (int i = 0; i < multiplierButtons.Length; i++)
            multiplierButtons[i].GetComponent<Image>().color = investmentMultiplier == (i == 0 ? 1 : i == 1 ? 5 : 10) ? new Color(.06f,.39f,.40f) : new Color(.10f,.135f,.17f);
        for (int i = 0; i < passiveCards.Length; i++)
        {
            bool unlocked = InventoryPresentation.PassiveUnlocked(growth, i);
            int required = i % 3 == 0 ? 20 : i % 3 == 1 ? 50 : 100;
            passiveCards[i].state.text = $"{required}P · {(unlocked ? "해금" : "미해금")}";
            passiveCards[i].state.color = unlocked ? new Color(.35f,1,.83f) : new Color(.59f,.65f,.70f);
            passiveCards[i].background.color = unlocked ? new Color(.07f,.19f,.20f) : new Color(.075f,.10f,.13f);
        }
        RefreshGrowthButtonFeedback();
    }
    private void InitializeGrowthButtonFeedback()
    {
        feedbackButtons = new[] { resetStatsButton, statCards[0].invest, statCards[1].invest, statCards[2].invest,
            characterTab, statTab, resetCancelButton, resetConfirmButton };
        feedbackBorders = new Image[feedbackButtons.Length]; feedbackLabels = new TMP_Text[feedbackButtons.Length];
        for (int i=0; i<feedbackButtons.Length; i++)
        {
            var button=feedbackButtons[i]; if(button==null) continue;
            button.transition=Selectable.Transition.None;
            button.targetGraphic.CrossFadeColor(Color.white,0,true,true);
            feedbackBorders[i]=button.transform.Find("Border")?.GetComponent<Image>();
            feedbackLabels[i]=button.GetComponentInChildren<TMP_Text>(true);
        }
    }
    public void SetGrowthButtonPointer(int index, bool over, bool pressed)
    {
        if(over) hoveredButton=index; else if(hoveredButton==index) hoveredButton=-1;
        if(pressed) pressedButton=index; else if(pressedButton==index) pressedButton=-1;
        RefreshGrowth();
    }
    public void ClearGrowthButtonPointer(int index)
    {
        // OnDisable can run inside SetActive; defer repaint until the next normal refresh.
        if(hoveredButton==index) hoveredButton=-1;
        if(pressedButton==index) pressedButton=-1;
    }
    public void ReleaseGrowthButton(int index)
    {
        if(pressedButton==index) pressedButton=-1;
        RefreshGrowth();
    }
    private string GrowthButtonHint()
    {
        if(feedbackButtons==null || hoveredButton<0 || hoveredButton>=feedbackButtons.Length) return null;
        var button=feedbackButtons[hoveredButton]; if(button==null || button.IsInteractable()) return null;
        if(hoveredButton==0)
        {
            long missing=CharacterSaveService.StatResetCost-rewards.Money;
            if(missing>0) return $"골드가 {missing:N0}G 부족합니다.";
            string reason;
            return CharacterSaveService.Current!=null && !CharacterSaveService.Current.CanResetStats(out reason) ? reason : null;
        }
        if(hoveredButton>=1 && hoveredButton<=3)
        {
            if(resetConfirming) return "초기화 확인 중에는 투자할 수 없습니다.";
            if(!growth.CanInvest) return "현재는 스탯을 투자할 수 없습니다.";
            if(growth.PointsIn((PlayerProgression.Stat)(hoveredButton-1))>=100) return "이 스탯은 100P 상한에 도달했습니다.";
            return "미사용 스탯 포인트가 없습니다.";
        }
        return null;
    }
    private void RefreshGrowthButtonFeedback()
    {
        if(feedbackButtons==null) return;
        for(int i=0;i<feedbackButtons.Length;i++)
        {
            var button=feedbackButtons[i]; if(button==null) continue;
            bool enabled=button.IsInteractable(), hover=hoveredButton==i, pressed=enabled&&pressedButton==i&&hover;
            bool plus=i>=1&&i<=3, selected=i==4&&!statPage.activeSelf || i==5&&statPage.activeSelf, confirmation=i==7;
            Color background=new Color(.09f,.12f,.16f), border=new Color(.28f,.32f,.34f), label=new Color(.82f,.87f,.90f);
            if(plus) border=new Color(.15f,.80f,.82f);
            if(selected) { background=new Color(.10f,.24f,.25f); border=new Color(.84f,.66f,.32f); }
            if(confirmation) { background=new Color(.24f,.19f,.10f);border=new Color(.84f,.66f,.32f); }
            if(!enabled)
            {
                background=hover?new Color(.14f,.17f,.20f):new Color(.065f,.08f,.10f);
                border=hover?new Color(.48f,.55f,.59f):new Color(.20f,.25f,.28f);
                label=hover?new Color(.66f,.73f,.77f):new Color(.38f,.44f,.47f);
            }
            else if(hover)
            {
                background=plus?new Color(.04f,.37f,.39f):selected?new Color(.16f,.34f,.36f):confirmation?new Color(.36f,.28f,.12f):new Color(.18f,.24f,.29f);
                border=plus?new Color(.30f,1f,.95f):selected||confirmation?new Color(1,.83f,.46f):new Color(.65f,.75f,.80f);
                label=Color.white;
            }
            if(pressed) { background=plus?new Color(.02f,.21f,.24f):new Color(.07f,.14f,.17f); label=new Color(.75f,.90f,.92f); }
            button.GetComponent<Image>().color=background;
            if(feedbackBorders[i]!=null) feedbackBorders[i].color=border;
            if(feedbackLabels[i]!=null) feedbackLabels[i].color=label;
        }
    }

    private void BeginStatReset()
    {
        var save = CharacterSaveService.Current; string error;
        if (!IsOpen || resetConfirming || resetBusy || save == null || !save.CanResetStats(out error)) return;
        resetConfirming = true; resetReviewedSnapshot = JsonUtility.ToJson(save.Capture());
        investmentError = null; HideTooltip(); RefreshGrowth();
    }
    private void CancelStatReset()
    {
        if (resetConfirming) investmentError = null;
        resetConfirming = false; resetReviewedSnapshot = null;
        if (resetDefault != null) { resetDefault.SetActive(true); resetConfirmation.SetActive(false); }
    }
    private void ConfirmStatReset()
    {
        if (!IsOpen || !resetConfirming || resetBusy) return;
        var save = CharacterSaveService.Current; string error;
        if (save == null) return;
        string latest = JsonUtility.ToJson(save.Capture());
        if (latest != resetReviewedSnapshot)
        {
            resetReviewedSnapshot = latest;
            investmentError = "상태가 변경되었습니다 · 내용을 확인한 뒤 다시 확인해 주세요";
            RefreshGrowth(); return;
        }
        if (!save.CanResetStats(out error)) { investmentError = error; RefreshGrowth(); return; }
        resetBusy = true;
        try
        {
            RefreshGrowth();
            if (!save.TryResetStats(out error)) investmentError = "저장 실패 · 초기화 미적용 · 재시도하거나 취소해 주세요";
            else
            {
                CancelStatReset(); investmentError = null;
            }
        }
        finally { resetBusy = false; RefreshGrowth(); RefreshStats(); }
    }
    private void RefreshStatReset()
    {
        if (resetStatsButton == null) return;
        var save = CharacterSaveService.Current; string error;
        bool allowed = save != null && save.CanResetStats(out error);
        resetDefault.SetActive(!resetConfirming); resetConfirmation.SetActive(resetConfirming);
        resetStatsButton.interactable = allowed && !resetBusy;
        resetConfirmButton.interactable = allowed && !resetBusy; resetCancelButton.interactable = !resetBusy;
        resetGoldText.text = $"보유 {rewards.Money:N0}G";
        resetSummary.text = $"투자 포인트 전부 반환 ({growth.SpentStatPoints}P) · 500G 소비";
    }
    private void OnRectTransformDimensionsChange() { if (grid != null) UpdateGrid(); if (tooltip != null) HideTooltip(); }
    private void UpdateGrid()
    {
        lastGridWidth = ((RectTransform)grid.transform).rect.width;
        float available = lastGridWidth - grid.padding.horizontal;
        int columns = Mathf.Clamp(Mathf.FloorToInt((available + grid.spacing.x) / 205f), 1, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns;
        float width = Mathf.Max(40, (available - grid.spacing.x * (columns - 1)) / columns);
        grid.cellSize = new Vector2(width, Mathf.Min(225, width * 1.12f));
    }
    public void Hover(RectTransform source, SkillData skill, string note, int slot)
    {
        HideTooltip(); hoverMaterial=null; hoverTarget = source; hoverSkill = skill; hoverNote = note; hoverSlot = slot; hoverAt = Time.unscaledTime + hoverDelay;
    }
    public void HoverMaterial(RectTransform source,MaterialData material){Hover(source,null,null,-1);hoverMaterial=material;}
    public void Leave(RectTransform source) { if (hoverTarget == source) HideTooltip(); }
    public void ShowExternalTooltip(RectTransform source,string description){Hover(source,null,description,-1);externalTooltip=true;}
    public void HideTooltip() { hoverTarget = null; externalTooltip=false; if (tooltip != null) tooltip.gameObject.SetActive(false); }
    private void ShowTooltip()
    {
        if ((!IsOpen&&!externalTooltip) || !hoverTarget.gameObject.activeInHierarchy) { HideTooltip(); return; }
        var skill = hoverSlot >= 0 ? player.Equipped(hoverSlot) : hoverSkill;
        tooltipText.text = skill != null ? InventoryPresentation.SkillTooltip(skill, player, CanEquip) : hoverNote;
        if(hoverMaterial!=null)tooltipText.text=InventoryPresentation.MaterialTooltip(hoverMaterial,MaterialQuantity(hoverMaterial.id));
        float width = Mathf.Min(450, canvasRect.rect.width - 32);
        float height = Mathf.Min(canvasRect.rect.height - 32, tooltipText.GetPreferredValues(tooltipText.text, width - 40, 0).y + 36);
        tooltip.sizeDelta = new Vector2(width, height);
        var corners = new Vector3[4]; hoverTarget.GetWorldCorners(corners);
        Vector2 right = canvasRect.InverseTransformPoint(corners[2]); Vector2 left = canvasRect.InverseTransformPoint(corners[1]);
        float x = right.x + 12;
        if (x + width > canvasRect.rect.xMax - 12) x = left.x - width - 12;
        x = Mathf.Clamp(x, canvasRect.rect.xMin + 12, canvasRect.rect.xMax - width - 12);
        float y = Mathf.Clamp(right.y, canvasRect.rect.yMin + height + 12, canvasRect.rect.yMax - 12);
        tooltip.anchoredPosition = new Vector2(x, y); tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling();
    }
    private void OnDisable()
    {
        CloseInternal();
        if (active == this) active = null;
    }
    private void OnDestroy()
    {
        if (session != null) session.Changed -= RefreshAll;
        if(rewards!=null)rewards.Changed-=RewardsChanged;
        if (run != null) run.RunEnded -= OnResult;
        if (health != null) health.Died -= OnPlayerDied;
        if (growth != null) growth.Changed -= RefreshGrowth;
        if (active == this) active = null;
    }
}
