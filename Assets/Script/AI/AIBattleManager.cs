using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents.Policies;

/// <summary>
/// 人間対学習済みML-Agentsのオフライン対戦を管理する。
/// HumanはDeck1、AIはDeck2を使用する。
/// </summary>
public class AIBattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MlAgents aiAgent;
    [SerializeField] private AIBattleVisual visual;

    [Header("Deck")]
    [SerializeField] private bool useSavedDecks = true;
    [SerializeField] private bool useBasicDeckWhenInvalid = true;

    [Header("Turn")]
    [SerializeField] private bool randomizeFirstPlayer;

    [Header("ML-Agents")]
    [Tooltip("ON: Python Trainerへ接続して人間との対戦を学習。OFF: ONNXで推論のみ。")]
    [SerializeField] private bool learnFromHuman;

    private GameManager gm;
    private Player humanPlayer;
    private Player aiPlayer;
    private int observedDecisionTick = -1;

    public event Action BoardChanged;

    public Player HumanPlayer => humanPlayer;
    public Player AIPlayer => aiPlayer;
    public GameManager Game => gm;
    public bool IsHumanTurn => gm != null && gm.turn == humanPlayer;
    public PhaseState CurrentPhase => gm != null ? gm.currentPhase : PhaseState.Start;
    public bool IsFinished => gm != null && gm.currentState == GameState.Finished;
    public bool LearnFromHuman => learnFromHuman;
    public int CompletedMatches { get; private set; }

    private void Start()
    {
        if (aiAgent == null || visual == null)
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
        if (gm == null || gm.decisionTick == observedDecisionTick) return;

        observedDecisionTick = gm.decisionTick;
        NotifyBoardChanged();
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }
    }

    private void StartBattle()
    {
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }

        List<Card> humanDeck = BuildDeck(
            useSavedDecks ? DeckManager.player1Deck : null);
        List<Card> aiDeck = BuildDeck(
            useSavedDecks ? DeckManager.player2Deck : null);

        humanPlayer = new Player(humanDeck);
        aiPlayer = new Player(aiDeck);

        bool aiGoesFirst = randomizeFirstPlayer && UnityEngine.Random.value < 0.5f;
        Player first = aiGoesFirst ? aiPlayer : humanPlayer;
        Player second = aiGoesFirst ? humanPlayer : aiPlayer;

        gm = new GameManager(first, second);
        gm.OnGameFinished += HandleGameFinished;
        observedDecisionTick = gm.decisionTick;

        ConfigureAgentBehavior();
        aiAgent.Initialize(aiPlayer, humanPlayer, gm);
        visual.Initialize(this);
        NotifyBoardChanged();
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
            return DeckManager.CreateBasicCardDeck();
        }

        throw new InvalidOperationException(
            "AI対戦用デッキが不正です。Deck1とDeck2を保存してください。");
    }

    private void ConfigureAgentBehavior()
    {
        BehaviorParameters behavior = aiAgent.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            Debug.LogError("AI AgentにBehaviorParametersがありません。");
            return;
        }

        // DefaultはPython Trainer接続時に学習し、未接続時はModelを使用する。
        // InferenceOnlyはInspectorに設定したONNXだけで動作する。
        behavior.BehaviorType = learnFromHuman
            ? BehaviorType.Default
            : BehaviorType.InferenceOnly;

        Debug.Log(learnFromHuman
            ? "【対人学習】Trainer接続待機モードで開始します。"
            : "【AI対戦】学習済みモデルの推論モードで開始します。");
    }

    public bool SubmitMarigan(List<Card> cards)
    {
        // 初手マリガンだけは先攻・後攻に関係なく同時進行する。
        if (gm == null || gm.currentState != GameState.WaitingForInput ||
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
        if (!ValidatePlayBasics(source, false, out reason)) return false;

        if (source.select != null && source.select.isSelectConstructor)
        {
            List<Card> pool = GetTargetPool(source.select.whereTarget);
            int required = source.select.numOfSelect;
            int validCount = pool.Count(candidate => source.ValidateTargets(
                humanPlayer, aiPlayer, new List<Card>() { candidate }));
            if (validCount < required)
            {
                reason = $"有効な対象が不足しています（必要:{required}、候補:{validCount}）。";
                return false;
            }
        }

        reason = null;
        return true;
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

        int required = source.select.numOfSelect;
        if (targets == null || targets.Count != required)
        {
            reason = $"対象数が不正です（必要:{required}、選択:{targets?.Count ?? 0}）。";
            return false;
        }
        if (targets.Any(card => card == null) ||
            targets.Distinct().Count() != targets.Count)
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
        return gm != null && gm.currentState == GameState.WaitingForInput &&
               gm.turn == humanPlayer;
    }

    private bool ExecuteHumanAction(PlayerAction action)
    {
        bool result = gm.ExecuteAction(humanPlayer, aiPlayer, action);
        observedDecisionTick = gm.decisionTick;
        NotifyBoardChanged();
        return result;
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
        if (gm == null || gm.currentState == GameState.Finished) return;
        gm.Surrender(humanPlayer);
    }

    private void HandleGameFinished(Player winner)
    {
        CompletedMatches++;
        NotifyBoardChanged();
        visual.ShowGameResult(winner == humanPlayer);
        Debug.Log(
            $"【対人学習】Episode {CompletedMatches} 終了 / " +
            $"AI結果:{(winner == aiPlayer ? "勝利" : "敗北")}");
    }

    private void NotifyBoardChanged()
    {
        BoardChanged?.Invoke();
        if (visual != null) visual.Refresh();
    }
}