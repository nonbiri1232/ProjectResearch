using UnityEngine;
using UnityEngine.UI; // Buttonを使うために必要です
using TMPro; // 【追加】TextMeshProを使うための宣言
using System.Collections.Generic;

public class GameVisual : MonoBehaviour
{
    [Header("System")]
    public Button endTurnButton;
    
    public Button selfGarbageButton;
    public TextMeshProUGUI systemText; // 【変更】Text から TextMeshProUGUI へ

    [Header("Player 1 UI")]
    public TextMeshProUGUI p1MemoryText; // 【変更】
    public Transform p1HandArea;

    [Header("Player 2 UI")]
    public TextMeshProUGUI p2MemoryText; // 【変更】
    public Transform p2HandArea;

    [Header("Prefabs")]
    public GameObject cardButtonPrefab; 

    private GameManager gm;
    private Player player1;
    private Player player2;

    void Start()
    {
        player1 = new Player(CreateDummyDeck());
        player2 = new Player(CreateDummyDeck());

        gm = new GameManager(player1, player2);

        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        selfGarbageButton.onClick.AddListener(OnSelfGarbageClicked);

        UpdateUI();
    }

    public void OnEndTurnClicked()
    {
        PlayerAction action = new PlayerAction();
        action.type = ActionType.End;

        gm.ExecuteAction(gm.turn, GetEnemyPlayer(), action); 
        UpdateUI();
    }

    public void OnSelfGarbageClicked()
    {
        PlayerAction action = new PlayerAction();
        action.type = ActionType.SelfGarbage;

        gm.ExecuteAction(gm.turn, GetEnemyPlayer(), action); 
        UpdateUI();
    }

    private void UpdateUI()
    {
        systemText.text = gm.turn == player1 ? "Player 1 Turn" : "Player 2 Turn";
        p1MemoryText.text = $"P1 Memory: {player1.fieldCost} / {player1.maxMemory} \n {player1.usedMemory} / {player1.usableMemory}";
        p2MemoryText.text = $"P2 Memory: {player2.fieldCost} / {player2.maxMemory} \n {player2.usedMemory} / {player2.usableMemory}";

        DrawHand(player1, p1HandArea);
        DrawHand(player2, p2HandArea);
    }

    private void DrawHand(Player targetPlayer, Transform handArea)
    {
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        foreach (Card c in targetPlayer.hand)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, handArea);
            
            // 【変更】子オブジェクトから取得するコンポーネントも TextMeshProUGUI にします
            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // \n は改行のマークです
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}"; 
        }
    }

    private Player GetEnemyPlayer()
    {
        return gm.turn == player1 ? player2 : player1;
    }

    private List<Card> CreateDummyDeck()
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 20; i++) {
            deck.Add(new IncrementProcess());
        }
        return deck;
    }
}