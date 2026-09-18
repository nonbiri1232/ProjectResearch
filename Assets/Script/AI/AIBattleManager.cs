using System;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents.Policies;

/// <summary>
/// 人間対学習済みML-Agentsのオフライン対戦を管理する。
/// 人間は保存済みDeck1、AIは指定された候補デッキからランダムに使用する。
/// </summary>
public class AIBattleManager : BattleManager
{
    [Header("References")]
    [SerializeField] private MlAgents aiAgent;
    [SerializeField] private AIBattleVisual visual;

    [Header("Deck")]
    [SerializeField] private bool useSavedDecks = true;
    [SerializeField] private bool useBasicDeckWhenInvalid = true;

    [Header("AI Deck Pool")]
    [Tooltip("AIに使用させるデッキを登録したカタログ")]
    [SerializeField] private AIDeckCatalog aiDeckCatalog;
    [Tooltip("AIへ渡す候補デッキID。空ならカタログ内の全デッキが候補")]
    [SerializeField] private List<int> aiDeckIds = new List<int>();

    [Header("Turn")]
    [SerializeField] private bool randomizeFirstPlayer;

    [Header("ML-Agents")]
    [Tooltip("ON: Python Trainerへ接続して人間との対戦を学習。OFF: ONNXで推論のみ。")]
    [SerializeField] private bool learnFromHuman;


    private Player humanPlayer;
    private Player aiPlayer;
    private int observedDecisionTick = -1;

    public event Action BoardChanged;

    public Player HumanPlayer => humanPlayer;
    public Player AIPlayer => aiPlayer;
    public GameManager Game => gm;
    public bool IsHumanTurn => gm != null && gm.turn == humanPlayer;
    public override PhaseState CurrentPhase => gm != null ? gm.currentPhase : PhaseState.Start;
    public override bool IsFinished => gm != null && gm.currentState == GameState.Finished;
    public bool LearnFromHuman => learnFromHuman;
    public int CompletedMatches { get; private set; }

    [Header("Shared battle view")]
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private PlayerInputManager inputManager;
    [SerializeField] private CardConect cardDatabase;
    [SerializeField] private CardLayoutManager p1HandLayout, p1FieldLayout, p1GarbageLayout, p1DeckLayout, p1MariganLayout;
    [SerializeField] private CardLayoutManager p2HandLayout, p2FieldLayout, p2GarbageLayout, p2DeckLayout;
    [SerializeField] private Transform enemyAttackTarget;
    private Player pendingWinner;
    private bool selectingGarbage;
    private UnityEngine.UI.Button garbageConfirmButton;
    private CardLayoutManager[] AllLayouts => new[] { p1HandLayout, p1FieldLayout, p1GarbageLayout,
        p1DeckLayout, p1MariganLayout, p2HandLayout, p2FieldLayout, p2GarbageLayout, p2DeckLayout };

    private void Start()
    {
        if (aiAgent == null || (visual == null && uiManager == null))
        {
            Debug.LogError("AIBattleManager: aiAgentまたはvisualが設定されていません。");
            enabled = false;
            return;
        }

        if (useSavedDecks)
        {
            DeckManager.LoadDeck();
        }

        StartBattle();
    }

    private void Update()
    {
        if (gm == null || isPresenting || gm.decisionTick == observedDecisionTick) return;

        observedDecisionTick = gm.decisionTick;
        NotifyBoardChanged();
    }

    public override void OnDestroy()
    {
        if (gm != null) gm.OnGameFinished -= HandleGameFinished;
        if (aiAgent != null)
        {
            aiAgent.ActionExecutor = null;
            aiAgent.CanRequestAction = null;
        }
        base.OnDestroy();
    }

    private void StartBattle()
    {
        pendingWinner = null;
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }

        List<Card> humanDeck = BuildDeck(
            useSavedDecks ? DeckManager.player1Deck : null);
        List<Card> aiDeck = BuildAIDeck(out int selectedAIDeckId);

        Debug.Log(selectedAIDeckId >= 0
            ? $"【AIデッキ選択】Deck ID: {selectedAIDeckId}"
            : "【AIデッキ選択】フォールバックデッキを使用します。");

        humanPlayer = new Player(humanDeck);
        aiPlayer = new Player(aiDeck);
        localPlayer = humanPlayer;
        remotePlayer = aiPlayer;

        bool aiGoesFirst = randomizeFirstPlayer && UnityEngine.Random.value < 0.5f;
        Player first = aiGoesFirst ? aiPlayer : humanPlayer;
        Player second = aiGoesFirst ? humanPlayer : aiPlayer;

        gm = new GameManager(first, second);
        gm.OnGameFinished += HandleGameFinished;
        observedDecisionTick = gm.decisionTick;

        if (!ConfigureAgentBehavior()) return;
        aiAgent.ActionExecutor = action => ExecutePresentedAction(aiPlayer, humanPlayer, action);
        aiAgent.CanRequestAction = () => !isPresenting;
        aiAgent.Initialize(aiPlayer, humanPlayer, gm);
        if (uiManager != null) InitializeBattleView();
        else visual.Initialize(this);
        NotifyBoardChanged();
    }

    private List<Card> BuildAIDeck(out int selectedDeckId)
    {
        if (aiDeckCatalog != null &&
            aiDeckCatalog.TryCreateRandomDeck(aiDeckIds, out List<Card> deck,
                out selectedDeckId))
        {
            return deck;
        }

        selectedDeckId = -1;
        return BuildDeck(useSavedDecks ? DeckManager.player2Deck : null);
    }

    private List<Card> BuildDeck(List<int> ids)
    {
        if (ids != null && ids.Count > 0)
        {
            List<Card> savedDeck = Player.ChangeCard(ids.ToArray());
            if (savedDeck.Count >= 5) return savedDeck;
        }

        if (useBasicDeckWhenInvalid)
        {
            // The old basic fallback consists of plain Card instances and has no
            // card abilities.  Prefer a real catalog deck so an empty PlayerPrefs
            // save on a new PC does not silently create an effect-less match.
            if (aiDeckCatalog != null &&
                aiDeckCatalog.TryCreateRandomDeck(
                    new List<int> { 0 }, out List<Card> catalogFallback,
                    out int fallbackDeckId))
            {
                Debug.LogWarning(
                    $"保存デッキが見つからないため、実カードのDeck ID: {fallbackDeckId}を使用します。" +
                    "自作デッキを使うにはデッキ作成画面でDeck 1を保存してください。");
                return catalogFallback;
            }

            return DeckManager.CreateBasicCardDeck();
        }

        throw new InvalidOperationException(
            "対戦用デッキが不正です。AI Deck Catalogまたは保存デッキを設定してください。");
    }

    private bool ConfigureAgentBehavior()
    {
        BehaviorParameters behavior = aiAgent.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            Debug.LogError("AI AgentにBehaviorParametersがありません。");
            enabled = false;
            return false;
        }

        if (!learnFromHuman && behavior.Model == null)
        {
            Debug.LogError(
                "推論モードにはBehaviorParametersのModel設定が必要です。" +
                "学習する場合はLearn From HumanをONにしてください。");
            enabled = false;
            return false;
        }

        // DefaultはPython Trainer接続時に学習し、未接続時はModelを使用する。
        // InferenceOnlyはInspectorに設定したONNXだけで動作する。
        behavior.BehaviorType = learnFromHuman
            ? BehaviorType.Default
            : BehaviorType.InferenceOnly;

        Debug.Log(learnFromHuman
            ? "【対人学習】Trainer接続待機モードで開始します。"
            : "【AI対戦】学習済みモデルの推論モードで開始します。");
        return true;
    }

    public bool SubmitMarigan(List<Card> cards)
    {
        // 初手マリガンだけは先攻・後攻に関係なく同時進行する。
        if (isPresenting || gm == null || gm.currentState != GameState.WaitingForInput ||
            gm.currentPhase != PhaseState.Start || gm.systemTurn != 1 ||
            !gm.NeedsMarigan(humanPlayer))
        {
            return false;
        }

        return ExecuteHumanAction(new PlayerAction(
            ActionType.Marigan, cards ?? new List<Card>()));
    }

    public bool SubmitSelfGarbage(List<Card> cards)
    {
        if (!CanHumanAct() || gm.currentPhase != PhaseState.Start ||
            gm.systemTurn == 1)
        {
            return false;
        }

        return ExecuteHumanAction(new PlayerAction(
            ActionType.SelfGarbage, cards ?? new List<Card>()));
    }

    public bool CanBeginPlay(Card source, out string reason)
    {
        return ValidatePlayBasics(source, false, out reason);
    }

    public bool PlayCard(Card source, List<Card> targets = null, bool addCost = false)
    {
        if (!ValidatePlay(source, targets, addCost, out string reason))
        {
            Debug.LogWarning($"カードをプレイできません: {reason}");
            return false;
        }

        PlayerAction action = targets != null
            ? new PlayerAction(ActionType.Play, source, targets)
            : new PlayerAction(ActionType.Play, source);
        action.isAddCost = addCost;

        bool result = ExecuteHumanAction(action);
        if (!result)
        {
            Debug.LogWarning(
                $"GameManagerがプレイを拒否しました。Card:{source.GetType().Name}, " +
                $"Cost:{source.Cost}, AddCost:{addCost}, " +
                $"Field:{humanPlayer.fieldCost}/{humanPlayer.maxMemory}, " +
                $"Used:{humanPlayer.usedMemory}/{humanPlayer.usableMemory}");
        }
        return result;
    }

    private bool ValidatePlay(
        Card source, List<Card> targets, bool addCost, out string reason)
    {
        if (!ValidatePlayBasics(source, addCost, out reason)) return false;

        bool needsTargets = source.select != null && source.select.isSelectConstructor;
        if (!needsTargets)
        {
            if (targets != null && targets.Count > 0)
            {
                reason = "このカードは対象を選択しません。";
                return false;
            }
            reason = null;
            return true;
        }

        int selectedCount = targets?.Count ?? 0;
        if (selectedCount == 0)
        {
            reason = null;
            return true;
        }
        if (selectedCount > source.select.numOfSelect)
        {
            reason = $"対象数が上限を超えています（上限:{source.select.numOfSelect}、選択:{selectedCount}）。";
            return false;
        }
        if (targets.Any(card => card == null) ||
            targets.Distinct().Count() != selectedCount)
        {
            reason = "対象にnullまたは重複があります。";
            return false;
        }

        List<Card> pool = GetTargetPool(source.select.whereTarget);
        if (targets.Any(card => !pool.Contains(card)))
        {
            reason = "対象の選択領域が正しくありません。";
            return false;
        }
        if (!source.ValidateTargets(humanPlayer, aiPlayer, targets))
        {
            reason = "カード固有の対象条件を満たしていません。";
            return false;
        }

        reason = null;
        return true;
    }

    private bool ValidatePlayBasics(Card source, bool addCost, out string reason)
    {
        if (!CanHumanAct())
        {
            reason = "現在はあなたが行動できる状態ではありません。";
            return false;
        }
        if (gm.currentPhase != PhaseState.Main)
        {
            reason = "メインフェーズではありません。";
            return false;
        }
        if (source == null || !humanPlayer.hand.Contains(source) ||
            source.player != humanPlayer)
        {
            reason = "カードがあなたの手札にありません。";
            return false;
        }

        int totalCost = source.Cost + (addCost ? 1 : 0);
        if (humanPlayer.fieldCost + totalCost > humanPlayer.maxMemory)
        {
            reason = $"フィールドメモリが不足しています（必要:{totalCost}）。";
            return false;
        }
        if (humanPlayer.usedMemory + totalCost > humanPlayer.usableMemory)
        {
            int remaining = humanPlayer.usableMemory - humanPlayer.usedMemory;
            reason = $"使用可能メモリが不足しています（必要:{totalCost}、残り:{remaining}）。";
            return false;
        }

        bool ignoreAssert = humanPlayer.field.Any(card => card is ForcedDebugMode);
        if (source.isAssert && !ignoreAssert && humanPlayer.maxMemory > source.Assert)
        {
            reason = $"Assert条件を満たしていません（最大メモリを{source.Assert}以下にしてください）。";
            return false;
        }
        // AddCost()はGameManager本処理で呼ばれるため、事前判定では
        // 現在存在する副作用なしの固有条件だけを確認する。
        if (source is DeepArchive && humanPlayer.garbage.Count < 10)
        {
            reason = "DeepArchiveにはガベージが10枚以上必要です。";
            return false;
        }

        reason = null;
        return true;
    }

    private List<Card> GetTargetPool(where targetArea)
    {
        switch (targetArea)
        {
            case where.hand:
                return humanPlayer.hand;
            case where.selfField:
                return humanPlayer.field;
            case where.enemyField:
                return aiPlayer.field;
            default:
                return new List<Card>();
        }
    }

    public bool Attack(Card attacker, Card target = null)
    {
        if (!CanHumanAct() || gm.currentPhase != PhaseState.Main ||
            attacker == null || !humanPlayer.field.Contains(attacker))
        {
            return false;
        }

        PlayerAction action = target == null
            ? new PlayerAction(ActionType.Attack, attacker)
            : new PlayerAction(
                ActionType.Attack, attacker, new List<Card>() { target });
        return ExecuteHumanAction(action);
    }

    public bool EndTurn()
    {
        if (!CanHumanAct() || gm.currentPhase != PhaseState.Main) return false;
        return ExecuteHumanAction(new PlayerAction(ActionType.End));
    }

    private bool CanHumanAct()
    {
        return !isPresenting && gm != null && gm.currentState == GameState.WaitingForInput &&
               gm.turn == humanPlayer;
    }

    private bool ExecuteHumanAction(PlayerAction action)
    {
        return ExecutePresentedAction(humanPlayer, aiPlayer, action);
    }

    public void StartNextBattle()
    {
        if (gm != null && gm.currentState != GameState.Finished)
        {
            Debug.LogWarning("対戦中は次の対戦を開始できません。");
            return;
        }

        StartBattle();
    }

    public void SurrenderHuman()
    {
        if (isPresenting || gm == null || gm.currentState == GameState.Finished) return;
        gm.Surrender(humanPlayer);
    }

    private void HandleGameFinished(Player winner)
    {
        CompletedMatches++;
        NotifyBoardChanged();
        pendingWinner = winner;
        if (!isPresenting) ShowResult();
        string mode = learnFromHuman ? "対人学習" : "AI対戦";
        Debug.Log(
            $"【{mode}】Episode {CompletedMatches} 終了 / " +
            $"AI結果:{(winner == aiPlayer ? "勝利" : "敗北")}");
    }

    private void NotifyBoardChanged()
    {
        if (isPresenting) return;
        BoardChanged?.Invoke();
        if (uiManager != null) SyncBattleVisuals();
        else if (visual != null) visual.Refresh();
    }

    public override void SubmitPlay(CardData sourceData, bool addCost, List<CardData> targetDatas = null)
    {
        Card source = humanPlayer.hand.FirstOrDefault(c => c.uniqueId == sourceData.uniqueId);
        var pool = humanPlayer.hand.Concat(humanPlayer.field).Concat(aiPlayer.field).ToList();
        List<Card> targets = ResolveCards(targetDatas, pool);
        if (targetDatas != null && targets == null) { p1HandLayout?.RefreshCard(); return; }
        if (!PlayCard(source, targets, addCost)) p1HandLayout?.RefreshCard();
    }

    public override void SubmitAttack(CardData attackerData, CardData? targetData = null)
    {
        if (!CanAttackTarget(attackerData, targetData)) return;
        Attack(humanPlayer.field.First(c => c.uniqueId == attackerData.uniqueId),
            targetData.HasValue ? aiPlayer.field.First(c => c.uniqueId == targetData.Value.uniqueId) : null);
    }

    public override void SubmitEndTurn() { EndTurn(); }

    public override void SubmitMarigan(List<CardData> data)
    {
        List<Card> cards = ResolveCards(data, humanPlayer.hand);
        if (cards != null) SubmitMarigan(cards);
    }

    public override void SubmitSelfGarbage(List<CardData> data)
    {
        List<Card> cards = ResolveCards(data, humanPlayer.field);
        if (cards != null) SubmitSelfGarbage(cards);
    }

    private static List<Card> ResolveCards(List<CardData> data, List<Card> pool)
    {
        var result = new List<Card>();
        if (data == null) return result;
        foreach (CardData item in data)
        {
            Card card = pool.FirstOrDefault(c => c.uniqueId == item.uniqueId);
            if (card == null || result.Contains(card)) return null;
            result.Add(card);
        }
        return result;
    }

    private bool ExecutePresentedAction(Player actor, Player enemy, PlayerAction action)
    {
        if (isPresenting) return false;
        bool useView = uiManager != null;
        bool attack = action.type == ActionType.Attack && action.sourceCard != null;
        CardData source = action.sourceCard != null ? Card.PackingCard(action.sourceCard) : default;
        CardLayoutManager field = actor == humanPlayer ? p1FieldLayout : p2FieldLayout;
        CardLayoutManager targetField = actor == humanPlayer ? p2FieldLayout : p1FieldLayout;
        Vector3 targetPosition = targetField != null ? targetField.CenterPosition : Vector3.zero;
        if (attack && action.targetCard != null && action.targetCard.Count > 0)
        {
            GameObject obj = targetField?.FindCardObject(Card.PackingCard(action.targetCard[0]));
            if (obj != null) targetPosition = obj.transform.position;
        }
        else if (attack && actor == humanPlayer && enemyAttackTarget != null)
            targetPosition = enemyAttackTarget.position;

        // Suppress board/result refresh raised synchronously inside ExecuteAction.
        isPresenting = useView;
        bool success = gm.ExecuteAction(actor, enemy, action);
        observedDecisionTick = gm.decisionTick;
        if (!success || !useView)
        {
            isPresenting = false;
            NotifyBoardChanged();
            ShowResult();
            return success;
        }
        if (attack && field != null)
            field.PlayAttack(source, targetPosition, () => StartCoroutine(FinishPresentation()));
        else if (action.type == ActionType.Play && field != null)
        {
            Card card = action.sourceCard;
            CardSetting setting = cardDatabase != null
                ? cardDatabase.cards.FirstOrDefault(c => c.className == card.GetType().Name) : null;
            field.PresentPlay(actor == humanPlayer ? p1HandLayout : p2HandLayout,
                Card.PackingCard(card), actor == humanPlayer, setting?.ability, setting?.cardImage);
            // Reveal even a Method that immediately goes to garbage before syncing.
            StartCoroutine(FinishPresentation(0.55f));
        }
        else
            StartCoroutine(FinishPresentation());
        return true;
    }

    private System.Collections.IEnumerator FinishPresentation(float beforeSync = 0f)
    {
        if (beforeSync > 0f) yield return new WaitForSeconds(beforeSync);
        SyncBattleVisuals();
        // Layout movement takes 0.5 seconds; include deaths, draws and zone changes.
        yield return new WaitForSeconds(0.55f);
        isPresenting = false;
        UpdateControls();
        BoardChanged?.Invoke();
        ShowResult();
    }

    private void InitializeBattleView()
    {
        StopAllCoroutines();
        isPresenting = false;
        pendingWinner = null;
        selectingGarbage = false;
        inputManager.ResetSelection();
        inputManager.battleManager = this;
        uiManager.battleManager = this;
        uiManager.inputManager = inputManager;
        if (uiManager.endGame != null) uiManager.endGame.SetActive(false);
        if (visual != null) visual.enabled = false;
        foreach (var layout in AllLayouts)
        {
            if (layout == null) continue;
            layout.ClearCards();
            layout.Initialize();
        }
        if (garbageConfirmButton == null)
        {
            garbageConfirmButton = CreateButton("選択してガベージ確定", uiManager.endTurnButton.transform.parent,
                new Vector2(0.84f, 0.58f), () => inputManager.ConfirmSelfGarbage());
            CreateButton("再戦", uiManager.endGame.transform, new Vector2(0.5f, 0.25f), StartNextBattle);
            CreateButton("投了", uiManager.endTurnButton.transform.parent, new Vector2(0.93f, 0.95f), SurrenderHuman);
        }
        uiManager.ShowMarigan();
        foreach (var layout in AllLayouts) layout?.BeginBatchUpdate();
        SyncBattleVisuals();
        foreach (var layout in AllLayouts) layout?.EndBatchUpdate();
        inputManager.StartMariganSelection();
        isPresenting = true;
        StartCoroutine(FinishPresentation());
    }

    private UnityEngine.UI.Button CreateButton(string label, Transform parent, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        var button = Instantiate(uiManager.endTurnButton, parent);
        button.name = label;
        // Clear serialized callbacks inherited from the template too.
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        button.interactable = true;
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(240, 55);
        var text = button.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null) { text.text = label; text.fontSize = 22; }
        button.gameObject.SetActive(true);
        return button;
    }

    private void SyncBattleVisuals()
    {
        bool marigan = gm.NeedsMarigan(humanPlayer);
        if (!marigan) uiManager.HideMarigan();
        SyncPlayer(humanPlayer, p1DeckLayout, marigan ? p1MariganLayout : p1HandLayout,
            p1FieldLayout, p1GarbageLayout);
        SyncPlayer(aiPlayer, p2DeckLayout, p2HandLayout, p2FieldLayout, p2GarbageLayout);
        uiManager.UpdateUI(gm, humanPlayer, aiPlayer);
        UpdateControls();
    }

    private void SyncPlayer(Player owner, CardLayoutManager deck, CardLayoutManager hand,
        CardLayoutManager field, CardLayoutManager garbage)
    {
        var zones = new[] { owner.deck, owner.hand, owner.field, owner.garbage };
        var destinations = new[] { deck, hand, field, garbage };
        var liveIds = new HashSet<int>(zones.SelectMany(z => z).Select(c => c.uniqueId));
        bool ownsScope = gm.currentScope != null && gm.currentScope.player == owner;
        if (ownsScope) liveIds.Add(gm.currentScope.uniqueId);
        var ownerLayouts = owner == humanPlayer
            ? new[] { p1DeckLayout, p1HandLayout, p1FieldLayout, p1GarbageLayout, p1MariganLayout }
            : new[] { p2DeckLayout, p2HandLayout, p2FieldLayout, p2GarbageLayout };
        foreach (var layout in ownerLayouts) layout?.RetainCards(liveIds);
        for (int i = 0; i < zones.Length; i++)
            foreach (Card card in zones[i]) SyncCard(owner, card, destinations[i]);
        if (ownsScope) SyncCard(owner, gm.currentScope, field);
    }

    private void SyncCard(Player owner, Card card, CardLayoutManager destination)
    {
        if (destination == null) return;
        CardData data = Card.PackingCard(card);
        if (destination.FindCardObject(data) == null)
        {
            var source = AllLayouts.FirstOrDefault(l => l != null && l.FindCardObject(data) != null);
            if (source != null) destination.ReceiveCard(source, data, source.FindCardObject(data));
            else destination.CreateCard(data);
        }
        CardSetting setting = cardDatabase != null
            ? cardDatabase.cards.FirstOrDefault(c => c.className == card.GetType().Name) : null;
        bool visible = !destination.IsFaceDown;
        destination.UpdateCard(data, owner == humanPlayer, visible ? setting?.ability : string.Empty,
            visible, visible ? setting?.cardImage : null);
    }

    private void UpdateControls()
    {
        bool garbage = !IsFinished && gm.turn == humanPlayer && gm.currentPhase == PhaseState.Start && gm.systemTurn > 1;
        if (garbage && !selectingGarbage) inputManager.StartSelfGarbageSelection();
        selectingGarbage = garbage;
        if (garbageConfirmButton != null)
        {
            garbageConfirmButton.gameObject.SetActive(garbage);
            garbageConfirmButton.interactable = !isPresenting;
        }
        uiManager.endTurnButton.interactable = CanAct() && CurrentPhase == PhaseState.Main && !IsFinished;
        if (uiManager.mariganConfirmButton != null)
            uiManager.mariganConfirmButton.interactable = !isPresenting && gm.NeedsMarigan(humanPlayer);
    }

    private void ShowResult()
    {
        if (pendingWinner == null || isPresenting) return;
        if (uiManager != null)
        {
            inputManager.ResetSelection();
            uiManager.HideMarigan();
            if (garbageConfirmButton != null) garbageConfirmButton.gameObject.SetActive(false);
            uiManager.endGame.SetActive(true);
            uiManager.endText.text = pendingWinner == humanPlayer ? "勝利" : "敗北";
        }
        else visual?.ShowGameResult(pendingWinner == humanPlayer);
    }

}
