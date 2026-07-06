using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
public enum GameState
{
    Processing,
    WaitingForInput,
    Finished
}

public enum ActionType
{
    Marigan,
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
    public bool isAddCost;
    
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
    
    public PlayerAction(ActionType ty,List<Card> target)
    {
        type = ty;
        targetCard = new List<Card>();
        targetCard.AddRange(target);
    }
    public PlayerAction(ActionType ty,Card source,List<Card> target)
    {
        type = ty;
        sourceCard = source;
        targetCard = new List<Card>();
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
    public PhaseState currentPhase;
    List<Player> Didmarigan = new List<Player>();
    List<PlayLog> logs = new List<PlayLog>();

    public GameManager(Player first,Player second)
    {
        player1 = first;
        player2 = second;
        Didmarigan.Add(first);
        Didmarigan.Add(second);
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
        turn = move;
        currentPhase = PhaseState.Start;
        systemTurn++;
        move.usableMemory = ++move.turn;
        move.usedMemory = 0;
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
                if (c.IsFailSafe())
                {
                    move.DoFailSafe(wait,c);
                    WriteLog(LogType.FailSafe,c);
                    break;
                }
            }
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return;
        }
        if(turn.field.Count == 0 && systemTurn != 1)
        {
            MainPhase(move,wait);
        }
        WriteLog(LogType.TurnStart);
        currentState = GameState.WaitingForInput;
    }

    public void MainPhase(Player move,Player wait)
    {
        currentPhase = PhaseState.Main;    
    }

    public void EndPhase(Player move,Player wait)
    {
        currentPhase = PhaseState.End;
        if(turn.field.Count > 0)
        {
            foreach(var c in turn.field.ToList())
            {
                if(c.Type == Card.CardType.Object){
                    c.isFirstTurn = false;
                    c.isCanAttack = true;
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
                if(c.IsFailSafe())
                {
                    move.DoFailSafe(wait,c);
                    WriteLog(LogType.FailSafe,c);
                    break;
                }
            }
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return;
        }
        StartPhase(wait,move);
    }
    public void Surrender(Player surrenderPlayer)
    {
        winner = surrenderPlayer;
        currentState = GameState.Finished;
        OnGameFinished?.Invoke(winner);
    }
    public bool ExecuteAction(Player move,Player wait,PlayerAction action)
    {
        bool isCorrect = false;
        if(currentState != GameState.WaitingForInput) return false;

        currentState = GameState.Processing;

        switch (currentPhase)
        {
            case PhaseState.Start:
                switch(action.type)
                {
                    case ActionType.SelfGarbage:
                        move.DestoryField(wait,action.targetCard,true);                    
                        WriteLog(LogType.SelfDestory,action.targetCard);
                        MainPhase(move,wait);
                        isCorrect = true;
                        break;
                    case ActionType.Marigan:
                        if(systemTurn == 1 && Didmarigan.Contains(move)){
                            move.Marigan(action.targetCard);
                            WriteLog(LogType.Marigan,action.targetCard);
                            Didmarigan.Remove(move);
                            if(Didmarigan.Count == 0)
                            {    
                                MainPhase(move,wait);
                            }
                            isCorrect = true;
                        }                        
                        break;
                }
                break;
            case PhaseState.Main:
                switch (action.type)
                {
                    case ActionType.Attack:
                        isCorrect = Attack(move,wait,action);
                        if(isCorrect)
                            WriteLog(LogType.Attack,action.sourceCard,action.targetCard);
                        break;
                
                    case ActionType.Play:
                        isCorrect = Play(move,wait,action);
                        if(isCorrect)
                            WriteLog(LogType.PlayCard,action.sourceCard);
                        break;
                
                    case ActionType.End :
                        EndPhase(move,wait);
                        isCorrect = true;
                        break;
                }
                break;
        }
        if (IsFinish(move,wait))
        {
            FinishGame();
            return true;
        }
        
        currentState = GameState.WaitingForInput;
        return isCorrect;
        
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
    }

    //実体化の処理
    public bool Play(Player move,Player wait,PlayerAction action)
    {
        //プレイできるかを確認
        if (action.isAddCost)
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
        if (action.isAddCost)
        {
            action.sourceCard.isImmediate = true;
            action.sourceCard.Cost += 1;
            action.sourceCard.Attack += 1;
            action.sourceCard.Hp += 1;
            action.sourceCard.ChangeCost += 1;
            action.sourceCard.ChangeAttack += 1;
            action.sourceCard.ChangeHp += 1;
        }
        move.PlayFeild(action.sourceCard);
        if(currentScope != null)
        {
            currentScope.ScopeEffectOnPlay(wait,action.sourceCard);
        }
        cr.OnPlay(action.sourceCard);
        action.sourceCard.Constructor(wait,action.targetCard);
        action.sourceCard.OnPlay();
        return true;
    }

    //攻撃行動
    public bool Attack(Player move,Player wait,PlayerAction action)
    {
        var source = action.sourceCard;
        if(source.Type != Card.CardType.Object)
        {
            return false;
        }
        if (!source.isCanAttack)
        {
            return false;
        }
        //出たばかりのターンか？
        if (source.isFirstTurn && !source.isImmediate)
        {
            return false;
        }
        //このターンすでに攻撃しているか
        if (source.isAttacked >= source.attackTimes)
        {
            return false;
        }
        //直接攻撃できるか
        if(action.targetCard == null && wait.field.Count <= 0 && !source.isFirstTurn)
        {
            move.DirectAttack(wait,action.sourceCard);
            source.isEncrypted = false;
            source.isAttacked++;
            return true;

        }
        if(action.targetCard == null && !source.isFirstTurn)
        {
            return false;
        }
        if(action.targetCard == null && wait.field.Count >= 0)
        {
            return false;
        }
        List<Card> checkProxy = new List<Card>(wait.field);
        
        var target = action.targetCard[0];
        
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
        //ターゲットが暗号化されているか
        if (target.isEncrypted)
        {
            return false;
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
        if(target.Hp <= 0 || source.Hp <= 0)
        {
            if(target.Hp <= 0)
            {
                move.DestoryField(wait,action.targetCard);
            }
            if(source.Hp <= 0)
            {
                List<Card> sourceL = new List<Card>{action.sourceCard};
                wait.DestoryField(move,sourceL);
            }
            source.isEncrypted = false;
            source.isAttacked++;
            return true;
        }
        //HPの増減処理
        if(!target.isSandBox){
            target.Hp -= sourceAtk;
            target.ChangeHp -= sourceAtk;
        }
        if(!source.isSandBox){
            source.Hp -= targetAtk;
            source.ChangeHp -= targetAtk;
        }
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
        source.isEncrypted = false;
        source.isAttacked++;
        return true;
    }
    public void WriteLog(LogType type,Card ccard=null,List<Card> ccards=null)
    {
        if (ccard != null)
        {
            CardSnapshot scard = PlayLog.PackageData(ccard);
            if(ccards != null)
            {
                List<CardSnapshot> scards = new List<CardSnapshot>(PlayLog.PackageData(ccards));
                logs.Add(new PlayLog(systemTurn/2,turn == player1,type,scard));
                return;
            }
            logs.Add(new PlayLog(systemTurn/2,turn == player1,type,scard));
        }
        logs.Add(new PlayLog(systemTurn/2,turn == player1,type));
    }
    public void WriteLog(LogType type,List<Card> ccards)
    {
        if(ccards != null)
        {
            List<CardSnapshot> scards = new List<CardSnapshot>(PlayLog.PackageData(ccards));
            logs.Add(new PlayLog(systemTurn/2,turn == player1,type,scards.ToArray()));
            return;
        }
    }
}
