using UnityEngine;
using System.Collections.Generic;

public class TrainingArena : MonoBehaviour
{
    [Header("対局させる2体のAgent")]
    [SerializeField] private MlAgents agentA;
    [SerializeField] private MlAgents agentB;

    [Header("保存済みデッキを使うか(falseならBasicDeck)")]
    [SerializeField] private bool useSavedDecks = false;

    private GameManager gm;

    private void Start()
    {
        if (useSavedDecks)
        {
            DeckManager.LoadDeck();
        }
        StartNewMatch();
    }

    public void StartNewMatch()
    {
        List<Card> deckA = BuildDeck(null);
        List<Card> deckB = BuildDeck(null);

        Player playerA = new Player(deckA);
        Player playerB = new Player(deckB);

        gm = new GameManager(playerA, playerB);

        agentA.Initialize(playerA, playerB, gm);
        agentB.Initialize(playerB, playerA, gm);

        // 対局終了後、次の対局を自動で開始する
        gm.OnGameFinished += (winner) => StartNewMatch();

        Debug.Log($"【学習】対局開始！ deckA枚数:{deckA.Count} deckB枚数:{deckB.Count}"); // ←追加
    }

    private List<Card> BuildDeck(List<int> deckIds)
    {
        if (deckIds == null || deckIds.Count == 0)
        {
            return DeckManager.CreateBasicCardDeck();
        }
        return Player.ChangeCard(deckIds.ToArray());
    }


}