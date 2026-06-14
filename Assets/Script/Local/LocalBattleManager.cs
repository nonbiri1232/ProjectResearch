using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LocalBattleManager:NetworkBehaviour
{
    [Header("描画クラスへの参照")]
    [SerializeField] private LocalBattleVisual visualManager;
    private Dictionary<ulong, List<Card>> receivedDecks = new Dictionary<ulong, List<Card>>();
    private GameManager gm;
    private Player host;
    private Player client;
    public void SendMyDeckToHost(int[] myDeckIds)
    {
        Debug.Log("ホストへ自分のデッキを送信します。");
        SubmitDeckServerRpc(myDeckIds);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitDeckServerRpc(int[] deckData ,ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        List<Card> deck = ChangeCard(deckData);

        receivedDecks[senderId] = deck;
        Debug.Log($"プレイヤー {senderId} のデッキを受信しました！ (現在の受信数: {receivedDecks.Count} / 2)");

        if (receivedDecks.Count >= 2)
        {
            Debug.Log("【通信ログ】両プレイヤーのデッキが揃いました！バトルの準備を開始します！");
            
            GameStart();
        }
    }
    private void GameStart()
    {
        ulong hostId = NetworkManager.ServerClientId;
        ulong clientId = 0;
        foreach (ulong id in receivedDecks.Keys)
        {
            if (id != hostId)
            {
                clientId = id;
                break;
            }
        }
        List<Card> hostDeck = receivedDecks[hostId];
        List<Card> clientDeck = receivedDecks[clientId];

        host = new Player(hostDeck);
        client = new Player(clientDeck);

        Player first = SelectFirstPlayer();
        gm = new GameManager(first,GetEnemyPlayer(first));
        
        int[] hostHand = transCardId(host.hand);

    }
    [ClientRpc]
    private void SetupBoardClientRpc()
    {
        
    }
    private int[] transCardId(List<Card> cards)
    {
        int[] c = new int[2];
        return c;   
    } 
    private Player SelectFirstPlayer()
    {
        int rnd = Random.Range(0,2);
        switch (rnd)
        {
            case 0:return host;
            case 1:return client;
        }
        return host;
    }
    private Player GetEnemyPlayer()
    {
        return gm.turn == host ? client : host;
    }
    private Player GetEnemyPlayer(Player pl)
    {
        return pl == host ? client : host;
    }
    private static List<Card> ChangeCard(int[] deckData)
    {
        List<Card> deck = new List<Card>();
        foreach(int i in deckData)
        {
            Card c = DeckManager.CreateCardInstance(i);
            deck.Add(c);
        }
        return deck;
    }
}
