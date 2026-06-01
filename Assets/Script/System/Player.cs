using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using System.Linq;

public class Player
{
    public int turn = 0;
    public int maxMemory = 20; //使用可能メモリ
    public int usableMemory = 0; //現在使えるメモリ
    public int fieldCost = 0;
    public int usedMemory = 0;

    public List<Card> hand = new List<Card>();
    public List<Card> deck = new List<Card>();
    public List<Card> garbage = new List<Card>();
    public List<Card> field = new List<Card>();
    public GameManager gm;
    public Player(List<Card> Deck)
    {
        deck.AddRange(Deck);
        foreach(Card c in Deck)
        {
            c.player = this;
        }
    }
    public void Marigan(List<Card> cards)
    {
        Draw(cards.Count);
        Debug.Log($"{cards.Count}枚マリガンしました");
        foreach(var c in cards)
        {
            hand.Remove(c);
            deck.Add(c);
            Debug.Log($"{c}をデッキに戻しました");
        }
        Shuffle();
    }
    public void DirectAttack(Player enemy,Card attacker)
    {
        maxMemory += attacker.Attack;
        enemy.maxMemory -= attacker.Attack;
    }

    private Card RandomSelect(List<Card> target)
    {
        int size = target.Count;
        int rnd = Random.Range(0,size);
        return target[rnd];
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
        if(hand.Count >= 8)
        {
            garbage.Add(c);
        }
        else
        {
            hand.Add(c);
        }
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
            if(hand.Count >= 8)
            {
                garbage.Add(c);
            }
            else
            {
                hand.Add(c);
            }
        }
        return false;
    }

    public void DestoryField(Player enemy,List<Card> target,bool isStartPhase = false)
    {
        if(target == null)
        {
            return;
        }
        foreach(var c in target.ToList())
        {
            if(c.isDaemon == true)
            {
                c.player.field.Remove(c);
                c.Destructor(c.player==this ? enemy: this);
                c.player.fieldCost -= c.Cost;
            }
            else
            {
                c.player.garbage.Add(c);
                c.player.field.Remove(c);
                c.Destructor(c.player==this ? enemy: this);
                c.player.fieldCost -= c.Cost;
                c.player.maxMemory -= c.Cost;
                maxMemory += c.Cost;
            }
            //変更されたステータスの修正
            c.Cost -= c.ChangeCost;
            c.Attack -= c.ChangeAttack;
            c.Hp -= c.ChangeHp;

            c.ChangeCost = 0;
            c.ChangeAttack = 0;
            c.ChangeHp = 0;
            c.isDaemon = c.Daemon;
            c.isEncrypted = c.Encrypted;
            c.isImmediate = c.Immediate;
            c.isProxy = c.Proxy;
            c.isSandBox = c.SandBox;
            c.isSegfault = c.Segfault;

        }
        if (isStartPhase && target.Count > 0)
        {
            DrawG();
        }
    }

    public void DoFailSafe(Player enemy,Card c)
    {
        c.ChangeCost += -c.Cost;
        c.Cost = 0;
        field.Add(c);
        deck.Remove(c);
        c.Constructor(enemy);
        c.OnPlay();
        c.FailSafe(enemy);
        Debug.Log($"フェイルセーフが発動しました。");
    }

    public void DrawG()
    {
        if(garbage.Count <= 0)return;
        var c = RandomSelect(garbage);
        if(hand.Count >= 8)
        {
            return;
        }
        garbage.Remove(c);
        hand.Add(c);
    }
    public void PlayFeild(Card c)
    {
        usedMemory += c.Cost;
        if(c.Type == Card.CardType.Scope)
        {
            gm.currentScope = c;
        }
        else
        {            
            field.Add(c);
            fieldCost += c.Cost;
            hand.Remove(c);
        }
    }   
}
