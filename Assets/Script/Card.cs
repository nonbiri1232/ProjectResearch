using UnityEngine;
public class Card
{
    public enum CardType{Object,Method,Scope}

    public bool isFirstTurn{get;protected set;} = true;
    public bool isProxy{get;protected set;}
    public bool isDaemon{get;protected set;}
    public bool isSandBox{get;protected set;}
    public bool isSegfault{get;protected set;}
    public bool isEncrypted{get;protected set;}
    public bool isImmediate{get;protected set;}
    public int attackTimes{get;protected set;} = 1;
    public CardType Type {get;protected set;}
    public int Cost{get; set;}
    public int Attack{get; set;}
    public int Hp{get;set;}
    protected Card RandomSelect(Card[] target)
    {
        int size = target.Length;
        int rnd = Random.Range(0,size);
        return target[rnd];
    }
    public void OnPlay()
    {
        if (isImmediate)
        {
            isFirstTurn = false;
        }      
    }
    public virtual void Constructor(GameManager mg ,Card[] target = null){}
    public virtual void Destructor(GameManager mg ,Card[] target = null){}
    public virtual void Assert(GameManager mg,Card[] target = null){}
    public virtual void FailSafe(GameManager mg,Card[] target = null){}
    public virtual void OnTurnStart(GameManager mg ,Card[] target = null){}
    public virtual void OnTurnEnd(GameManager mg ,Card[] target = null){}
    public virtual void OnAttack(GameManager mg ,Card[] target = null){}
}
