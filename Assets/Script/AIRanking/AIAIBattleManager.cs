using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

/// <summary>
/// 新AI・旧AIを自由に組み合わせて自動対戦を管理し、勝率を集計するマネージャー。
/// 描画（UI）処理を完全に排除し、高速なシミュレーションに特化しています。
/// </summary>
public class AIAIBattleManager : MonoBehaviour
{
    [Header("AI 1 (1P側) - どちらか片方をセット")]
    [SerializeField] private MlAgents ai1_New; 
    [SerializeField] private LegacyMlAgents ai1_Legacy; 

    [Header("AI 2 (2P側) - どちらか片方をセット")]
    [SerializeField] private MlAgents ai2_New; 
    [SerializeField] private LegacyMlAgents ai2_Legacy; 

    [Header("Match Settings")]
    [Tooltip("自動で対戦を行う最大回数")]
    public int maxMatches = 100000;
    [SerializeField] private bool randomizeFirstPlayer = true;

    [Header("AI Deck Pool")]
    [SerializeField] private AIDeckCatalog aiDeckCatalog;
    [SerializeField] private List<int> ai1DeckIds = new List<int>();
    [SerializeField] private List<int> ai2DeckIds = new List<int>();

    private GameManager gm;
    private Player aiPlayer1;
    private Player aiPlayer2;

    // 集計用データ
    public int CompletedMatches { get; private set; }
    public int AI1Wins { get; private set; }
    public int AI2Wins { get; private set; }

    // エラー防止用のフラグ
    private bool isStartingNextMatch;

    private void Start()
    {
        // どちらのAIもセットされていない場合はエラー
        if ((ai1_New == null && ai1_Legacy == null) || (ai2_New == null && ai2_Legacy == null))
        {
            Debug.LogError("AIAIBattleManager: 1P側または2P側のAIが設定されていません。");
            enabled = false;
            return;
        }

        // 超高速化: Unityのゲーム進行スピードを100倍にする
        Time.timeScale = 100f; 

        Debug.Log($"【AI自動対戦】 描画なし・高速モードで {maxMatches}回のテストを開始します。");
        StartNewMatch();
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }
        
        // 途中で止めた時用に時間を元に戻す
        Time.timeScale = 1f; 
    }

    public void StartNewMatch()
    {
        isStartingNextMatch = false;

        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }

        // デッキの構築
        List<Card> ai1Deck = BuildAIDeck(ai1DeckIds);
        List<Card> ai2Deck = BuildAIDeck(ai2DeckIds);

        aiPlayer1 = new Player(ai1Deck);
        aiPlayer2 = new Player(ai2Deck);

        // 先攻後攻の決定
        bool p1GoesFirst = randomizeFirstPlayer ? UnityEngine.Random.value < 0.5f : true;
        Player first = p1GoesFirst ? aiPlayer1 : aiPlayer2;
        Player second = p1GoesFirst ? aiPlayer2 : aiPlayer1;

        gm = new GameManager(first, second);

        // 1P側の初期化（新旧どちらがセットされているかで分岐）
        if (ai1_New != null)
        {
            ConfigureAgentBehavior(ai1_New);
            ai1_New.Initialize(aiPlayer1, aiPlayer2, gm);
        }
        else if (ai1_Legacy != null)
        {
            ConfigureAgentBehavior(ai1_Legacy);
            ai1_Legacy.Initialize(aiPlayer1, aiPlayer2, gm);
        }

        // 2P側の初期化（新旧どちらがセットされているかで分岐）
        if (ai2_New != null)
        {
            ConfigureAgentBehavior(ai2_New);
            ai2_New.Initialize(aiPlayer2, aiPlayer1, gm);
        }
        else if (ai2_Legacy != null)
        {
            ConfigureAgentBehavior(ai2_Legacy);
            ai2_Legacy.Initialize(aiPlayer2, aiPlayer1, gm);
        }

        gm.OnGameFinished += HandleGameFinished;
    }

    private List<Card> BuildAIDeck(List<int> deckIds)
    {
        if (aiDeckCatalog != null &&
            aiDeckCatalog.TryCreateRandomDeck(deckIds, out List<Card> deck, out int _))
        {
            return deck;
        }
        return DeckManager.CreateBasicCardDeck();
    }

    // Agent（ML-Agentsの基底クラス）を受け取るように変更し、新旧両方に対応
    private void ConfigureAgentBehavior(Agent agent)
    {
        BehaviorParameters behavior = agent.GetComponent<BehaviorParameters>();
        if (behavior != null)
        {
            behavior.BehaviorType = BehaviorType.Default; 
        }
    }

    private void HandleGameFinished(Player winner)
    {
        if (isStartingNextMatch) return;
        isStartingNextMatch = true;

        CompletedMatches++;

        if (winner == aiPlayer1)
        {
            AI1Wins++;
        }
        else if (winner == aiPlayer2)
        {
            AI2Wins++;
        }

        if (CompletedMatches % 1000 == 0 || CompletedMatches >= maxMatches)
        {
            float winRate1 = (float)AI1Wins / CompletedMatches * 100f;
            float winRate2 = (float)AI2Wins / CompletedMatches * 100f;
            
            Debug.Log($"【AI対戦進捗】 {CompletedMatches}戦 終了\n" +
                      $"AI 1 (勝率: {winRate1:F2}%) - {AI1Wins}勝\n" +
                      $"AI 2 (勝率: {winRate2:F2}%) - {AI2Wins}勝");
        }

        if (CompletedMatches < maxMatches)
        {
            StartCoroutine(StartNextMatchCoroutine());
        }
        else
        {
            Debug.Log("【テスト完了】10万回のAI対戦が終了しました。");
            Time.timeScale = 1f; 
        }
    }

    private IEnumerator StartNextMatchCoroutine()
    {
        yield return null;
        StartNewMatch();
    }
}