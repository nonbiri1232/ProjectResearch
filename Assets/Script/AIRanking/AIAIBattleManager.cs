using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Policies;

/// <summary>
/// AI同士（ML-Agents）の自動対戦を管理し、勝率を集計するマネージャー。
/// 描画（UI）処理を完全に排除し、高速なシミュレーションに特化しています。
/// </summary>
public class AIAIBattleManager : MonoBehaviour
{
    [Header("AI Agents")]
    [SerializeField] private MlAgents aiAgent1; // 1P側のAI
    [SerializeField] private MlAgents aiAgent2; // 2P側のAI

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

    private void Start()
    {
        if (aiAgent1 == null || aiAgent2 == null)
        {
            Debug.LogError("AIAIBattleManager: aiAgent1 または aiAgent2 が設定されていません。");
            enabled = false;
            return;
        }

        // ★超高速化: Unityのゲーム進行スピードを100倍にする
        // (ML-Agentsの学習/推論を高速で回すための定石です)
        Time.timeScale = 100f; 

        Debug.Log($"【AI自動対戦】 描画なし・高速モードで {maxMatches}回のテストを開始します。");
        StartBattle();
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

    private void StartBattle()
    {
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }

        // AI1とAI2のデッキをそれぞれ構築する
        List<Card> ai1Deck = BuildAIDeck(ai1DeckIds);
        List<Card> ai2Deck = BuildAIDeck(ai2DeckIds);

        aiPlayer1 = new Player(ai1Deck);
        aiPlayer2 = new Player(ai2Deck);

        // 先攻後攻の決定
        bool p1GoesFirst = randomizeFirstPlayer ? UnityEngine.Random.value < 0.5f : true;
        Player first = p1GoesFirst ? aiPlayer1 : aiPlayer2;
        Player second = p1GoesFirst ? aiPlayer2 : aiPlayer1;

        // ゲームマネージャーの初期化とイベント購読
        gm = new GameManager(first, second);
        gm.OnGameFinished += HandleGameFinished;

        // AIの振る舞いを設定（学習用か推論用か）
        ConfigureAgentBehavior(aiAgent1);
        ConfigureAgentBehavior(aiAgent2);

        // 各AIに、自分と相手のプレイヤー情報、およびGameManagerを渡して初期化
        aiAgent1.Initialize(aiPlayer1, aiPlayer2, gm);
        aiAgent2.Initialize(aiPlayer2, aiPlayer1, gm);
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

    private void ConfigureAgentBehavior(MlAgents agent)
    {
        BehaviorParameters behavior = agent.GetComponent<BehaviorParameters>();
        if (behavior == null)
        {
            Debug.LogError($"{agent.name} に BehaviorParameters がありません。");
            enabled = false;
            return;
        }
        
        // 自動対戦時は環境に合わせて Default か InferenceOnly を設定します
        behavior.BehaviorType = BehaviorType.Default; 
    }

    private void HandleGameFinished(Player winner)
    {
        CompletedMatches++;

        // 勝敗の集計
        if (winner == aiPlayer1)
        {
            AI1Wins++;
        }
        else if (winner == aiPlayer2)
        {
            AI2Wins++;
        }

        // ★高速化: 100戦ごとだとログが多すぎるので、1000戦ごとにログを出力
        if (CompletedMatches % 1000 == 0 || CompletedMatches >= maxMatches)
        {
            float winRate1 = (float)AI1Wins / CompletedMatches * 100f;
            float winRate2 = (float)AI2Wins / CompletedMatches * 100f;
            
            Debug.Log($"【AI対戦進捗】 {CompletedMatches}戦 終了\n" +
                      $"AI 1 (勝率: {winRate1:F2}%) - {AI1Wins}勝\n" +
                      $"AI 2 (勝率: {winRate2:F2}%) - {AI2Wins}勝");
        }

        // 指定回数に到達していなければ、次のゲームを自動で即座に開始する
        if (CompletedMatches < maxMatches)
        {
            StartBattle();
        }
        else
        {
            Debug.Log("【テスト完了】10万回のAI対戦が終了しました。");
            Time.timeScale = 1f; // 終わったらゲーム時間を元に戻す
        }
    }
}