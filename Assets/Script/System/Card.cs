using System;
using System.Collections.Generic;
public enum where
{
    None,
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
        whereTarget = where.None;
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
    private Random rand = new Random();
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
        if(target == null || target.Count == 0)return null;
        if(target == null)return null;
        int size = target.Count;
        int rnd = rand.Next(0,size);
        return target[rnd];
    }

    public void SettingBasicCard(int i)
    {
        Cost = i;
        Attack = i;
        Hp = i;
        Type = CardType.Object;
        select = new Select();
    }

    public static string GetCardClassName(int cardId)
    {
        switch (cardId)
        {
            case 0: return "SledOverClock";
            case 1: return "IncrementProcess";
            case 2: return "ClockDownBot";
            case 3: return "ParallelCompilation";
            case 4:return "PoisonPoint";
            case 5: return "UnSafeArea";
            case 6: return "Master";
            case 7: return "Raid10";
            case 8: return "RmRf";
            case 9: return "Paging";
            case 10: return "BackGroundMiner";
            case 11: return "SystemFreeze";
            case 12: return "CarnelPanicZero";
            case 13: return "AllDelete";
            default:
                return null;
        }
    }
    public static int GetCardId(string className)
    {
        switch (className)
        {
            case "SledOverClock": return 0;
            case "IncrementProcess": return 1;
            case "ClockDownBot": return 2;
            case "ParallelCompilation": return 3;
            case "PoisonPoint": return 4;
            case "UnSafeArea": return 5;
            case "Master": return 6;
            case "Raid10": return 7;
            case "RmRf": return 8;
            case "Paging": return 9;
            case "BackGroundMiner": return 10;
            case "SystemFreeze": return 11;
            case "CarnelPanicZero": return 12;
            case "AllDelete": return 13;
            default:
                return -1;
        }
    }
    public static int GetCardId(Card c)
    {
        string className = c.GetType().Name;
        switch (className)
        {
            case "SledOverClock": return 0;
            case "IncrementProcess": return 1;
            case "ClockDownBot": return 2;
            case "ParallelCompilation": return 3;
            case "PoisonPoint": return 4;
            case "UnSafeArea": return 5;
            case "Master": return 6;
            case "Raid10": return 7;
            case "RmRf": return 8;
            case "Paging": return 9;
            case "BackGroundMiner": return 10;
            case "SystemFreeze": return 11;
            case "CarnelPanicZero": return 12;
            case "AllDelete": return 13;
            default:
                return -1;
        }
    }
    public static Card CreateCardInstance(string className)
    {
        switch (className)
        {
            case "SledOverClock": return new SledOverClock();
            case "IncrementProcess": return new IncrementProcess();
            case "ClockDownBot": return new ClockDownBot();
            case "ParallelCompilation": return new ParallelCompilation();
            case "PoisonPoint": return new PoisonPoint();
            case "UnSafeArea": return new UnSafeArea();
            case "Master": return new Master();
            case "Raid10": return new Raid10();
            case "RmRf": return new RmRf();
            case "Paging": return new Paging();
            case "BackGroundMiner": return new BackGroundMiner();
            case "SystemFreeze": return new SystemFreeze();
            case "CarnelPanicZero": return new CarnelPanicZero();
            case "AllDelete": return new AllDelete();
            default:
                return null;
        }
    }
    public static Card CreateCardInstance(int cardId)
    {
        switch (cardId)
        {
            case 0: return new SledOverClock();
            case 1: return new IncrementProcess();
            case 2: return new ClockDownBot();
            case 3: return new ParallelCompilation();
            case 4:return new PoisonPoint();
            case 5: return new UnSafeArea();
            case 6: return new Master();
            case 7: return new Raid10();
            case 8: return new RmRf();
            case 9: return new Paging();
            case 10: return new BackGroundMiner();
            case 11: return new SystemFreeze();
            case 12: return new CarnelPanicZero();
            case 13: return new AllDelete();
            default:
                return null;
        }
    }
    
}
