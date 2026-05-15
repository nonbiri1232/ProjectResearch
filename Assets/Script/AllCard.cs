using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.U2D.IK;

public class SledOverClock : Card
{
    public SledOverClock()
    {
        Cost = 3;
        Attack = 5;
        Hp = 1;
        Type = CardType.Object;
    }
    private List<Card> costDownCard = new List<Card>();

    public override void Constructor(Player Enemy,List<Card> target)
    {
        cr.effectOnAttack.Add(this);
        cr.effectOnPlay.Add(this);
        foreach(Card c in player.hand)
        {
            costDownCard.Add(c);
            c.Cost += -2;
            c.ChangeCost += -2;
        }
    }
    public override void StartPhase(Player Enemy)
    {
        cr.effectOnAttack.Add(this);
        cr.effectOnPlay.Add(this);
    }
    public override void EndPhase(Player Enemy)
    {
        cr.effectOnAttack.Remove(this);
        cr.effectOnPlay.Remove(this);
    }

    public override void CrestOnPlay(Player Enemy, Card target = null)
    {
        target.isImmediate = true;
    }
    public override void CrestOnAttack(Player Enemy, List<Card> target = null)
    {
        target[0].isSandBox = true;
    }
}
public class IncrementProcess : Card
{
    public IncrementProcess()
    {
        Cost = 1;
        Attack =1;
        Hp = 1;
        Type = CardType.Object;
    }
    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        Card actualTarget = null;
        if(target != null && target.Count > 0)
        {
            actualTarget = target[0];
        }
        else
        {
            if(player.field.Count > 0)
            {
                actualTarget = RandomSelect(player.field);
            }
        }
        if(actualTarget != null){
            actualTarget.Attack += 1;
            actualTarget.Hp += 1;
        }
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        player.Draw();
    }

}
public class ClockDownBot : Card
{
    public ClockDownBot()
    {
        Cost = 1;
        Attack = 1;
        Hp = 1;
        Type = CardType.Object;
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        Card actualTarget = null;
        if(target != null && target.Count > 0)
        {
            actualTarget = target[0];
        }
        else
        {
            if(Enemy.field.Count > 0)
            {
                actualTarget = RandomSelect(Enemy.field);
            }
        }
        if(actualTarget != null){
            actualTarget.Attack -= 3;
        }
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        player.Draw();
    }
}

public class ParallelCompilation : Card
{
    public ParallelCompilation()
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
    public UnSafeArea()
    {
        Cost = 3;
        Type = CardType.Scope;
    }

    public override void ScopeEffectOnPlay(Player pl,Card target = null)
    {
        if(target != null){
            target.isImmediate = true;
            target.OnPlay();
        }
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
    public Master()
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
        c1.ChangeCost -= c1.Cost;
        c2.ChangeCost -= c2.Cost;
        c1.Cost = 0;
        c2.Cost = 0;
        var action1 = new PlayerAction(ActionType.Play,c1);
        var action2 = new PlayerAction(ActionType.Play,c2);
        player.gm.Play(player,Enemy,action1);
        player.gm.Play(player,Enemy,action2);
    }
}
public class Raid10 : Card
{
    public Raid10()
    {
        Cost = 8;
        Attack = 10;
        Hp = 5;
        isSandBox = true;
        attackTimes = 2;
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        if(player.field.Count > 0){
            var c1 = RandomSelect(player.field);
            c1.Attack += 5;
            c1.Hp += 5;
            c1.ChangeAttack += 5;
            c1.ChangeHp += 5;
        }
    }

    public override void StartPhase(Player Enemy)
    {
        isSandBox = true;
    }
}



