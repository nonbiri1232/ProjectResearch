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
        select = new Select();
    }

    public override void Constructor(Player Enemy,List<Card> target)
    {
        cr.effectOnAttack.Add(this);
        cr.effectOnPlay.Add(this);
        foreach(Card c in player.hand)
        {
            if(c.Cost >= 5)
            {
                c.ChangeCost -= c.Cost;
                c.Cost -= 2;
            }
            else
            {
                c.ChangeCost -= c.Cost;
                c.Cost -= c.Cost;
            }
        }
        isImmediate = true;
    }
    public override void StartPhase(Player Enemy)
    {
        cr.effectOnAttack.Add(this);
        cr.effectOnPlay.Add(this);
    }
    public override void Destructor(Player Enemy, List<Card> target = null)
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
        select = new Select(where.selfField,1);
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
            actualTarget.ChangeAttack += 1;
            actualTarget.Hp += 1;
            actualTarget.ChangeHp += 1;
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
        select = new Select(where.enemyField,1);
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
        if(actualTarget != null)
        {
            if(actualTarget.Attack >= 3)
            {
                actualTarget.Attack -= 3;
                actualTarget.ChangeAttack -= 3;
            }
            else
            {
                actualTarget.ChangeAttack -= actualTarget.Attack;
                actualTarget.Attack = 0;
            }
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
        select = new Select();
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        foreach(var c in player.field)
        {
            c.Attack += 3;
            c.ChangeAttack += 3;
            c.Hp += 3;
            c.ChangeHp += 3;
            c.isImmediate = true;
        }
    }
}
public class PoisonPoint : Card
{
    public PoisonPoint()
    {
        Cost = 2;
        Attack = 0;
        Hp = 2;
        Type = CardType.Object;
        select = new Select(where.selfField,1);
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
            actualTarget.isSegfault = true;
            actualTarget.isDaemon = true;
        }
        isSegfault = true;
        isDaemon = true;
    }
}
public class UnSafeArea : Card
{
    public UnSafeArea()
    {
        Cost = 3;
        Type = CardType.Scope;
        select = new Select();
    }

    public override void ScopeEffectOnPlay(Player pl,Card target = null)
    {
        if(target != null){
            target.isImmediate = true;
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
        select = new Select();
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
        if(randaomCard.Count >= 2)
        {
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
        else if(randaomCard.Count == 1)
        {
            var c1 = randaomCard[0];
            c1.ChangeCost -= c1.Cost;
            c1.Cost = 0;
            var action1 = new PlayerAction(ActionType.Play,c1);
            player.gm.Play(player,Enemy,action1);
        }
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
        select = new Select();
        Type = CardType.Object;
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
public class RmRf : Card
{
    public RmRf()
    {
        Cost = 20;
        Attack = 10;
        Hp = 3;
        Type = CardType.Object;
        select = new Select();
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        List<Card> targetList = new List<Card>(Enemy.field);
        player.DestoryField(Enemy, targetList);
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        Card c = RandomSelect(Enemy.field);
        List<Card> list = new List<Card>(){c};
        player.DestoryField(Enemy,list);
    }
}
public class Paging : Card
{
    public Paging()
    {
        Cost = 2;
        Type = CardType.Method;
        select = new Select(where.hand,1);
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        player.deck.Add(target[0]);
        player.hand.Remove(target[0]);
        player.Shuffle();
        player.Draw(2);
    }
}
public class BackGroundMiner : Card
{
    public BackGroundMiner()
    {
        isDaemon = true;
        isProxy = true;

        Cost = 3;
        Attack = 2;
        Hp = 2;
        Type = CardType.Object;
        select = new Select();
    }
    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        Enemy.maxMemory -= 2;
        player.maxMemory += 2;
    }
}
public class SystemFreeze : Card
{
    public SystemFreeze()
    {
        Cost = 5;
        Attack = 1;
        Hp = 1;
        Type = CardType.Object;
        select = new Select();
    }

    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        List<Card> targetList = new List<Card>(Enemy.field);
        player.DestoryField(Enemy, targetList);
    }
}
public class CarnelPanicZero : Card
{
    public CarnelPanicZero()
    {
        Cost = 1;
        Attack = 20;
        Hp = 20;
        Type = CardType.Object;
        select = new Select();
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        if(player.maxMemory != 1)
        {
            player.field.Remove(this);
            player.deck.Add(this);
            player.Shuffle();
        }
    }
    public override void Destructor(Player Enemy, List<Card> target = null)
    {
        player.garbage.Remove(this);
    }
    public override bool IsFailSafe()
    {
        if(player.maxMemory == 1)return true; 
        if(player.maxMemory > 0 && player.maxMemory <= 3)return true;
        if(player.maxMemory > 0 && player.maxMemory <= 5)return true;
        return false;
    }
    public override void FailSafe(Player Enemy, List<Card> target = null)
    {
        
        if(player.maxMemory > 0 && player.maxMemory <= 5)
        {
            if(Enemy.field.Count > 0){
                var c = RandomSelect(Enemy.field);
                c.isCanAttack = false;
            }
        }
        if(player.maxMemory > 0 && player.maxMemory <= 3)
        {
            if(player.hand.Count > 0)
            {
                foreach(var c in player.hand)
                {
                    if(c.Cost >= 5)
                    {
                        c.ChangeCost -= c.Cost;
                        c.Cost -= 5;
                    }
                    else
                    {
                        c.ChangeCost -= c.Cost;
                        c.Cost -= c.Cost;
                    }
                }
            }
        }
        if(player.maxMemory == 1)
        {
            List<Card> targetList = new List<Card>(Enemy.field);
            player.DestoryField(Enemy, targetList);
        }
        this.Constructor(Enemy ,target);
    }
}
public class AllDelete : Card
{
    public AllDelete()
    {
        isAssert = true;
        Assert = 10;

        Cost = 2;
        Type = CardType.Method;
        select = new Select();
    }

    public override void Constructor(Player Enemy, List<Card> target = null)
    {
        List<Card> allDelete = new List<Card>();
        if(player.field.Count > 0)
        {
            allDelete.AddRange(player.field);
        }
        if(Enemy.field.Count > 0)
        {
            allDelete.AddRange(Enemy.field);
        }
        player.DestoryField(Enemy,allDelete);
    }
}