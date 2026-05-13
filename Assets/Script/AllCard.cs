using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

public class SledOverClock : Card
{
    SledOverClock()
    {
        Cost = 3;
        Attack = 5;
        Hp = 1;
        Type = CardType.Object;
    }

    private bool isDownCost;
    private List<Card> costDownCard = new List<Card>();

    public override void Constructor(Player Enemy,List<Card> target)
    {
        foreach(Card c in player.hand)
        {
            costDownCard.Add(c);
            c.Cost -= 2;
            c.ChangeCost = -2;
        }
        isDownCost = true;
    }

    public override void EndPhase(Player Enemy,Card target = null)
    {
        if (isDownCost)
        {
            foreach(var c in costDownCard)
            {
                c.Cost += c.ChangeCost;
                c.ChangeCost = 0;
            }    
            costDownCard.Clear();
            isDownCost = false;
        }
    }
}
public class IncrementProcess : Card
{
    IncrementProcess()
    {
        Cost = 1;
        Attack =1;
        Hp = 1;
        Type = CardType.Object;
    }
    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        if(target[0] != null && target.Count > 0){
            target[0].Attack += 1;
            target[0].Hp += 1;
        }
        Attack += 1;
        Hp += 1;
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        player.Draw();
    }

}
public class ClockDownBot : Card
{
    ClockDownBot()
    {
        Cost = 1;
        Attack = 1;
        Hp = 1;
        Type = CardType.Object;
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        if(target[0] != null && target.Count > 0){
            target[0].Attack -= 3;
        }
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        player.Draw();
    }
}

public class ParallelCompilation : Card
{
    ParallelCompilation()
    {
        Cost = 5;
        Attack = 3;
        Hp = 3;
        Type = CardType.Object;
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        foreach(var c in player.field)
        {
            c.Attack += 3;
            c.Hp += 3;
            c.isImmediate = true;
            c.OnPlay();
        }
    }
}

public class UnSafeArea : Card
{
    UnSafeArea()
    {
        Cost = 3;
        Type = CardType.Scope;
    }

    public override void ScopeEffect(Player pl,List<Card> target = null)
    {
        target[0].isImmediate = true;
        target[0].OnPlay();
    }

    public override bool AddCost(Player Enemy, List<Card> target = null)
    {
        if(player.field.Count >= 3)
        {
            return true;
        }
        return false;
    }
}

public class Master : Card
{
    Master()
    {
        isProxy = true;
        isSegfault = true;

        Cost = 7;
        Attack = 1;
        Hp = 10;
        Type = CardType.Object;
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        List<Card> randaomCard = new List<Card>(); 
        foreach(var c in player.deck)
        {
            if(c.Cost >= 8)
            {
                randaomCard.Add(c);
            }
        }
        var c1 = RandomSelect(randaomCard);
        randaomCard.Remove(c1);
        var c2 = RandomSelect(randaomCard);
        randaomCard.Clear();
        var action1 = new PlayerAction(ActionType.Play,c1);
        var action2 = new PlayerAction(ActionType.Play,c2);
        player.gm.Play(player,Enemy,action1);
        player.gm.Play(player,Enemy,action2);
    }
}


