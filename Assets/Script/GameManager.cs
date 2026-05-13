using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using UnityEngine;

public class Player
{
    public int turn = 0;
    public int maxMemory = 20; //使用可能メモリ
    public int usableMemory = 0; //現在使えるメモリ

    public List<Card> hand = new List<Card>();
    public List<Card> deck = new List<Card>();
    public List<Card> garbage = new List<Card>();
    public List<Card> field = new List<Card>();

    public Player(Card[] Deck)
    {
        deck.AddRange(Deck);
    }

    public void Shuffle()
    {
        for(var i = deck.Count - 1;i > 0;i--){
            var j = Random.Range(0,i+1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    public bool Draw()
    {
        if(deck.Count <= 0)
        {
            return true;
        }

        var top = deck.Count-1;
        var c = deck[top];
        deck.RemoveAt(top);
        hand.Add(c);
        return false;
    }

    public bool Draw(int num)
    {
        for(int i = 0;i < num; i++)
        {
            if(deck.Count <= 0)
            {
                return true;
            }
            var top = deck.Count-1;
            var c = deck[top];
            deck.RemoveAt(top);
            hand.Add(c);
        }
        return false;
    }

    public void DestoryField(int[] target)
    {
        for(int i = 0;i < target.Length; i++)
        {
            Card c;
            if((c = field[target[i]]).isDaemon == true)
            {
                field.RemoveAt(target[i]);
            }
            else
            {
                garbage.Add(c);
                field.Remove(c);
            }
        }
    }
        
}

public enum GameState
{
    Processing,
    WaitingForInput
}
public class GameManager
{
    public GameState currentState;
    private const int maxHand = 8;
    Player player1;
    Player player2;

    public int systemTurn = 0;
    public bool isPlayer1Turn = true;

    public Card currentScope = null;

    public GameManager(Card[] deck1,Card[] deck2)
    {
        player1 = new Player(deck1);
        player2 = new Player(deck2);

        player1.Shuffle();
        player2.Shuffle();

        player1.Draw(4);
        player2.Draw(4);

        systemTurn++;
        StartPhase(player1,player2);
    }

    public void StartPhase(Player move,Player wait)
    {
        systemTurn++;
        move.turn++;
        if(systemTurn != 1)
        {
            move.Draw();
        }

        currentState = GameState.WaitingForInput;
    }

    public void ExecuteAction(Player player,Card cardToDestory)
    {
        if(currentState != GameState.WaitingForInput) return;

        currentState = GameState.Processing;

        
    }

    public void MainPhase(Player move,Player wait)
    {
        EndPhase(move,wait);
    }

    public void EndPhase(Player move,Player wait)
    {
        StartPhase(wait,move);
    }
}
