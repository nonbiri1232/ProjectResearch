using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
public enum where
{
    hand,
    selfField,
    enemyField
}
public class Select
{
    public bool isSelectConstructor{get;protected set;}
    public where whereTarget{get;protected set;}
    public int numOfSelect{get;protected set;}
    public Select()
    {
        isSelectConstructor = false;
    }
    public Select(where tar,int num)
    {
        isSelectConstructor = true;
        whereTarget = tar;
        numOfSelect = num;
    }

}

public class Card
{
    public enum CardType{Object,Method,Scope}

    public Player player;
    public Crest cr;
    public bool isCanAttack{get;set;} = true;
    public bool isFirstTurn{get;set;} = true;
    public bool Proxy{get;set;}
    public bool Daemon{get;set;}
    public bool SandBox{get;set;}
    public bool Segfault{get;set;}
    public bool Encrypted{get;set;}
    public bool Immediate{get;set;}
    public bool isProxy{get;set;}
    public bool isDaemon{get;set;}
    public bool isSandBox{get;set;}
    public bool isSegfault{get;set;}
    public bool isEncrypted{get;set;}
    public bool isImmediate{get;set;}
    public int attackTimes{get;protected set;} = 1;
    public bool isAssert{get;protected set;}
    public int Assert{get;protected set;}
    public int isAttacked{get;set;}
    public Select select;
    public CardType Type {get;protected set;}
    public int ChangeCost = 0;
    public int Cost{get; set;}
    public int ChangeAttack = 0;
    public int Attack{get; set;}
    public int ChangeHp = 0;
    public int Hp{get;set;}
    public void OnPlay()
    {
        Daemon = isDaemon;
        Encrypted = isEncrypted;
        Immediate = isImmediate;
        Proxy = isProxy;
        SandBox = isSandBox;
        Segfault = isSegfault;
    }
    public virtual bool AddCost(Player Enemy,List<Card> target = null){return true;}
    public virtual void Constructor(Player Enemy,List<Card> target = null){}
    public virtual void Destructor(Player Enemy,List<Card> target = null){}
    public virtual bool IsFailSafe(){return false;}
    public virtual void FailSafe(Player Enemy,List<Card> target = null){}
    public virtual void OnTurnStart(Player Enemy,List<Card> target = null){}
    public virtual void OnTurnEnd(Player Enemy,List<Card> target = null){}
    public virtual void OnAttack(Player Enemy,Card target = null){}
    public virtual void StartPhase(Player Enemy){}
    public virtual void EndPhase(Player Enemy){}
    public virtual void ScopeEffectOnAttack(Player pl,List<Card> target = null){}
    public virtual void ScopeEffectOnPlay(Player pl,Card target = null){}
    public virtual void CrestOnAttack(Player Enemy,List<Card> target = null){}
    public virtual void CrestOnPlay(Player Enemy,Card target = null){}

    protected Card RandomSelect(List<Card> target)
    {
        int size = target.Count;
        int rnd = Random.Range(0,size);
        return target[rnd];
    }
}
