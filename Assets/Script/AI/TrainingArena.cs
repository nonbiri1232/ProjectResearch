using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingArena : MonoBehaviour
{
    [Header("対局させる2体のAgent")]
    [SerializeField] private MlAgents agentA;
    [SerializeField] private MlAgents agentB;

    [Header("保存済みデッキを使うか(falseならBasicDeck)")]
    [SerializeField] private bool useSavedDecks = false;

    private GameManager gm;
    private bool isStartingNextMatch;

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
        isStartingNextMatch = false;

        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }

        List<Card> deckA = BuildDeck(
            useSavedDecks ? DeckManager.player1Deck : null);
        List<Card> deckB = BuildDeck(
            useSavedDecks ? DeckManager.player2Deck : null);

        Player playerA = new Player(deckA);
        Player playerB = new Player(deckB);

        gm = new GameManager(playerA, playerB);

        agentA.Initialize(playerA, playerB, gm);
        agentB.Initialize(playerB, playerA, gm);

        // Agentが終了報酬を処理した後、次フレームで次の対局を始める。
        gm.OnGameFinished += HandleGameFinished;

        Debug.Log($"【学習】対局開始！ deckA枚数:{deckA.Count} deckB枚数:{deckB.Count}");
    }

    private void HandleGameFinished(Player winner)
    {
        if (isStartingNextMatch) return;
        isStartingNextMatch = true;
        StartCoroutine(StartNextMatch());
    }

    private IEnumerator StartNextMatch()
    {
        yield return null;
        StartNewMatch();
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameFinished -= HandleGameFinished;
        }
    }

    private List<Card> BuildDeck(List<int> deckIds)
    {
        if (deckIds == null || deckIds.Count == 0)
        {
            return DeckManager.CreateBasicCardDeck();
        }

        List<Card> deck = Player.ChangeCard(deckIds.ToArray());
        // GameManagerは開始時に4枚引くため、5枚未満では初回判定で即終了する。
        return deck.Count >= 5 ? deck : DeckManager.CreateBasicCardDeck();
    }
}