using System;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
public enum GameState
{
    Processing,
    WaitingForInput,
    Finished
}

public enum ActionType
{
    SelfGarbage,
    Play,
    Attack,
    End
}
public enum PhaseState
{
    Start,
    Main,
    End
}

public class PlayerAction
{
    public ActionType type;
    public Card sourceCard;
    public List<Card> targetCard;
    
    public PlayerAction(){}
    public PlayerAction(ActionType ty)
    {
        type = ty;
    }
    
    public PlayerAction(ActionType ty,Card source)
    {
        type = ty;
       sourceCard = source;
    }
    
    public PlayerAction(ActionType ty,Card[] target)
    {
        type = ty;
        targetCard.AddRange(target);
    }
    public PlayerAction(ActionType ty,Card source,Card[] target)
    {
        type = ty;
        sourceCard = source;
        targetCard.AddRange(target);
    }
    
}

public class GameManager
{
    private Player winner;
    public event Action<Player> OnGameFinished;
    public GameState currentState;
    public Player turn;
    private const int maxHand = 8;
    Player player1;
    Player player2;
    Crest cr;
    public int systemTurn = 0;
    public bool isPlayer1Turn = true;
    public Card currentScope = null;
    private PhaseState currentPhase;

    public GameManager(Player first,Player second)
    {
        player1 = first;
        player2 = second;
        cr = new Crest(player1,player2);

        foreach(var c in player1.deck)c.cr = cr;
        foreach(var c in player2.deck)c.cr = cr;

        player1.Shuffle();
        player2.Shuffle();

        player1.Draw(4);
        player2.Draw(4);

        player1.gm = this;
        player2.gm = this;

        StartPhase(player1,player2);
    }

    public void StartPhase(Player move,Player wait)
    {
        Debug.Log("ターン開始");
        turn = move;
        currentPhase = PhaseState.Start;
        systemTurn++;
        move.usableMemory = ++move.turn;
        if(systemTurn != 1)
        {
            move.Draw();
        }
        if(turn.field.Count > 0)
        {
            foreach(var c in turn.field)
            {
                c.StartPhase(wait);
                c.isAttacked = 0;
            }
        }
        if(turn.deck.Count > 0)
        {
            foreach(var c in turn.deck)
            {
                if(c.IsFailSafe())move.DoFailSafe(wait,c);
            }
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return;
        }

        currentState = GameState.WaitingForInput;
    }

    public void MainPhase(Player move,Player wait)
    {
        
        Debug.Log("メインフェイズ");
        currentPhase = PhaseState.Main;    
    }

    public void EndPhase(Player move,Player wait)
    {
        
        Debug.Log("エンドフェイズ");
        currentPhase = PhaseState.End;
        if(turn.field.Count > 0)
        {
            foreach(var c in turn.field)
            {
                if(c.Type == Card.CardType.Object){
                    c.isFirstTurn = false;
                    c.EndPhase(wait);
                }
                else if(c.Type == Card.CardType.Method)
                {
                    List<Card> list = new List<Card>(){c};
                    c.EndPhase(wait);
                    c.player.DestoryField(wait,list);
                }
            }
        }
        if(turn.deck.Count > 0)
        {
            foreach(var c in turn.deck)
            {
                if(c.IsFailSafe())move.DoFailSafe(wait,c);
            }
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return;
        }
        StartPhase(wait,move);
    }

    public void ExecuteAction(Player move,Player wait,PlayerAction action)
    {
        if(currentState != GameState.WaitingForInput) return;

        currentState = GameState.Processing;

        switch (currentPhase)
        {
            case PhaseState.Start:
                if(action.type == ActionType.SelfGarbage)
                {
                    move.DestoryField(wait,action.targetCard,true);
                    MainPhase(move,wait);
                }
                break;
            case PhaseState.Main:
                switch (action.type)
                {
                    case ActionType.Attack:
                        Attack(move,wait,action);
                        break;
                
                    case ActionType.Play:
                        Play(move,wait,action);
                        break;
                
                    case ActionType.End :
                        EndPhase(move,wait);
                        break;
                }
                break;
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return;
        }
        
        currentState = GameState.WaitingForInput;
        
    }
    private bool IsFinish(Player pl1,Player pl2)
    {
        if(pl1.deck.Count <= 0){
            winner = pl2;
            return true;    
        }
        if(pl2.deck.Count <= 0)
        {
            winner = pl1;
            return true;
        }
        if(pl1.maxMemory <= 0)
        {
            winner = pl2;
            return true;
        }
        if(pl2.maxMemory <= 0)
        {
            winner = pl1;
            return true;
        }
        return false;
    }
    //終了処理
    private void FinishGame()
    {
        currentState = GameState.Finished;
        OnGameFinished?.Invoke(winner);

        Debug.Log($"ゲーム終了！勝者は {(winner == player1 ? "Player1" : "Player2")} です！");
    }

    //実体化の処理
    public bool Play(Player move,Player wait,PlayerAction action,bool isAddCost = false)
    {
        //プレイできるかを確認
        if (isAddCost)
        {
             if(move.fieldCost + action.sourceCard.Cost + 1 > move.maxMemory || move.usedMemory + action.sourceCard.Cost + 1 > move.usableMemory) return false;
            if(action.sourceCard.isAssert && move.maxMemory > action.sourceCard.Assert) return false;
            if(!action.sourceCard.AddCost(wait)) return false;
        }else{
            if(move.fieldCost + action.sourceCard.Cost > move.maxMemory || move.usedMemory + action.sourceCard.Cost > move.usableMemory) return false;
            if(action.sourceCard.isAssert && move.maxMemory > action.sourceCard.Assert) return false;
            if(!action.sourceCard.AddCost(wait)) return false;
        }
       
        //カードをプレイする。

        //追加コストを払うならコストを1上げて
        //攻撃と体力を+1/+1
        if (isAddCost)
        {
            action.sourceCard.Cost += 1;
            action.sourceCard.Attack += 1;
            action.sourceCard.Hp += 1;
            action.sourceCard.ChangeCost += 1;
            action.sourceCard.ChangeAttack += 1;
            action.sourceCard.ChangeHp += 1;
        }
        if(currentScope != null)
        {
            currentScope.ScopeEffectOnPlay(wait,action.sourceCard);
        }
        cr.OnPlay(action.sourceCard);
        action.sourceCard.Constructor(wait,action.targetCard);
        action.sourceCard.OnPlay();
        move.PlayFeild(action.sourceCard);
        return true;
    }

    //攻撃行動
    public bool Attack(Player move,Player wait,PlayerAction action)
    {
        List<Card> checkProxy = new List<Card>();
        var source = action.sourceCard;
        var target = action.targetCard[0];
        //出たばかりのターンか？
        if (source.isFirstTurn)
        {
            return false;
        }
        //このターンすでに攻撃しているか
        if (source.isAttacked >= source.attackTimes)
        {
            return false;
        }
        //プロキシがいるかを確認
        checkProxy.Remove(target);
        if(!target.isProxy){
            foreach(var c in checkProxy)
            {
                if (c.isProxy)
                {
                    return false;
                }
            }
        }
        int sourceAtk = source.Attack;
        int targetAtk = target.Attack;

        //能力の処理
        if(currentScope != null)
        {
            currentScope.ScopeEffectOnAttack(wait,action.targetCard);
        }
        cr.OnAttack(source,target);
        source.OnAttack(wait,target);
        if(target.Hp <= 0)
        {
            move.DestoryField(wait,action.targetCard);
        }
        //HPの増減処理
        if(!target.isSandBox)
            target.Hp -= sourceAtk;
        if(!source.isSandBox)
            source.Hp -= targetAtk;
        target.isSandBox = false;
        source.isSandBox = false;
        //オブジェクトの解放処理
        if(target.Hp <= 0 || source.isSegfault)
        {
            move.DestoryField(wait,action.targetCard);
        }
        if(source.Hp <= 0 || target.isSegfault)
        {
            List<Card> sourceL = new List<Card>{action.sourceCard};
            wait.DestoryField(move,sourceL);
        }
        source.isAttacked++;
        return true;
    }
}
