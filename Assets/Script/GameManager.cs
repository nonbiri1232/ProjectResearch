using System.Collections.Generic;
public enum GameState
{
    Processing,
    WaitingForInput
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
    public GameState currentState;
    public Player turn;
    private const int maxHand = 8;
    Player player1;
    Player player2;
    public int systemTurn = 0;
    public bool isPlayer1Turn = true;
    public Card currentScope = null;
    private PhaseState currentPhase;

    public GameManager(Card[] deck1,Card[] deck2)
    {
        player1 = new Player(deck1);
        player2 = new Player(deck2);

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
        if(systemTurn != 1)
        {
            move.Draw();
        }
        if(turn.field.Count > 0)
        {
            foreach(var c in turn.field)
            {
                c.isFirstTurn = false;
            }
        }

        currentState = GameState.WaitingForInput;
    }

    public void MainPhase(Player move,Player wait)
    {
        currentPhase = PhaseState.Main;    
    }

    public void EndPhase(Player move,Player wait)
    {
        currentPhase = PhaseState.End;
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
    }
    public void Play(Player move,Player wait,PlayerAction action)
    {
        if(move.fieldCost + action.sourceCard.Cost > move.maxMemory || move.usedMemory + action.sourceCard.Cost > move.usableMemory) return;
        if(action.sourceCard.isAssert && move.maxMemory > action.sourceCard.Assert) return;
        if(!action.sourceCard.AddCost(wait)) return;
        move.usedMemory += action.sourceCard.Cost;
        move.PlayFeild(action.sourceCard);
        action.sourceCard.Constructor(wait,action.targetCard);
        action.sourceCard.OnPlay();
        currentState = GameState.WaitingForInput;
    }

    public void Attack(Player move,Player wait,PlayerAction action)
    {
        var source = action.sourceCard;
        var target = action.targetCard[0];
        source.OnAttack(wait,target);
        if(target.Hp <= 0)
        {
            move.DestoryField(wait,action.targetCard);
        }
        if(!target.isSandBox)
            target.Hp -= source.Attack;
        if(!source.isSandBox)
            source.Hp -= target.Attack;
        if(target.Hp <= 0 || source.isSegfault)
        {
            move.DestoryField(wait,action.targetCard);
        }
        if(source.Hp <= 0 || target.isSegfault)
        {
            List<Card> sourceL = new List<Card>{action.sourceCard};
            wait.DestoryField(move,sourceL);
        }
        currentState = GameState.WaitingForInput;
        
    }
}
