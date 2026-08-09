using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MlAgents : Agent
{
    public Player myPlayer;
    public Player enemyPlayer;
    public GameManager gm;

    public void Initialize(Player me, Player enemy, GameManager manager)
    {
        myPlayer = me;
        enemyPlayer = enemy;
        gm = manager;
        gm.OnGameFinished += HandleGameFinished;
    }

    
    private int lastTick = -1;

    private void Update()
    {
        if (gm == null) return;

        if (gm.decisionTick != lastTick)
        {
            lastTick = gm.decisionTick;
            if (ComputeIsMyTurn())
            {
                RequestDecision();
            }
        }
    }

    private bool ComputeIsMyTurn()
    {
        if (gm.currentState != GameState.WaitingForInput) return false;

        if (gm.currentPhase == PhaseState.Start && gm.systemTurn == 1)
        {
            // 初手マリガンは同時進行
            return gm.NeedsMarigan(myPlayer);
        }

        // それ以外(自壊フェーズ・メインフェーズ)は通常のターン制
        return gm.turn == myPlayer;
    }



    private void HandleGameFinished(Player winner)
    {
        Debug.Log($"【学習】対局終了。勝者: {(winner == myPlayer ? "自分" : "相手")}"); // ←追加
        if (winner == myPlayer) AddReward(1.0f);
        else if (winner == enemyPlayer) AddReward(-1.0f);
        EndEpisode();
    }


    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (gm.currentPhase == PhaseState.Start)
        {
            if (gm.systemTurn == 1)
            {
                // マリガン専用フェーズ:Mariganだけ許可
                actionMask.SetActionEnabled(0, (int)ActionType.SelfGarbage, false);
                actionMask.SetActionEnabled(0, (int)ActionType.Play, false);
                actionMask.SetActionEnabled(0, (int)ActionType.Attack, false);
                actionMask.SetActionEnabled(0, (int)ActionType.End, false);
            }
            else
            {
                // 自壊専用フェーズ:SelfGarbageだけ許可
                actionMask.SetActionEnabled(0, (int)ActionType.Marigan, false);
                actionMask.SetActionEnabled(0, (int)ActionType.Play, false);
                actionMask.SetActionEnabled(0, (int)ActionType.Attack, false);
                actionMask.SetActionEnabled(0, (int)ActionType.End, false);
            }
        }
        else // Main
        {
            actionMask.SetActionEnabled(0, (int)ActionType.Marigan, false);
            actionMask.SetActionEnabled(0, (int)ActionType.SelfGarbage, false);

            // 手札が0枚なら「Play」をそもそも選べないようにする
            if (myPlayer.hand.Count == 0)
                actionMask.SetActionEnabled(0, (int)ActionType.Play, false);

            // 自分の場にカードが0枚なら「Attack」をそもそも選べないようにする
            if (myPlayer.field.Count == 0)
                actionMask.SetActionEnabled(0, (int)ActionType.Attack, false);
        }

        int handLimit = Mathf.Max(myPlayer.hand.Count, 1);
        for (int i = handLimit; i < 8; i++)
            actionMask.SetActionEnabled(1, i, false);

        int myFieldLimit = Mathf.Max(myPlayer.field.Count, 1);
        for (int i = myFieldLimit; i < 20; i++)
            actionMask.SetActionEnabled(2, i, false);

        int enemyFieldLimit = Mathf.Max(enemyPlayer.field.Count, 1);
        for (int i = enemyFieldLimit; i < 20; i++)
            actionMask.SetActionEnabled(3, i, false);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Agentの情報
        sensor.AddObservation(myPlayer.maxMemory);
        sensor.AddObservation(myPlayer.fieldCost);
        sensor.AddObservation(myPlayer.usedMemory);
        sensor.AddObservation(myPlayer.usableMemory);
        sensor.AddObservation(myPlayer.hand.Count);
        sensor.AddObservation(myPlayer.field.Count);
        sensor.AddObservation(myPlayer.deck.Count);
        sensor.AddObservation(myPlayer.garbage.Count);
        //Enemyの情報
        sensor.AddObservation(enemyPlayer.maxMemory);
        sensor.AddObservation(enemyPlayer.fieldCost);
        sensor.AddObservation(enemyPlayer.usedMemory);
        sensor.AddObservation(enemyPlayer.usableMemory);
        sensor.AddObservation(enemyPlayer.hand.Count);
        sensor.AddObservation(enemyPlayer.field.Count);
        sensor.AddObservation(enemyPlayer.deck.Count);
        sensor.AddObservation(enemyPlayer.garbage.Count);

        //手札の情報
        ObserveCard(sensor, myPlayer.hand, 8);
        //Agentのフィールドの情報
        ObserveCard(sensor, myPlayer.field, 20);
        //Enemyのフィールドの情報
        ObserveCard(sensor, enemyPlayer.field, 20);
    }

    private void ObserveCard(VectorSensor sensor, List<Card> cardList,int maxCapacity)
    {
        for(int i = 0; i < maxCapacity; i++)
        {
            if(i < cardList.Count)
            {
                Card card = cardList[i];
                sensor.AddObservation(Card.GetCardId(card));
                sensor.AddObservation(i);
                sensor.AddObservation(card.Attack);
                sensor.AddObservation(card.Cost);
                sensor.AddObservation(card.Hp);

                sensor.AddObservation(card.isDaemon);
                sensor.AddObservation(card.isEncrypted);
                sensor.AddObservation(card.isImmediate);
                sensor.AddObservation(card.isProxy);
                sensor.AddObservation(card.isSandBox);
                sensor.AddObservation(card.isSegfault);
                sensor.AddObservation(card.isCanAttack);
                sensor.AddObservation(card.isFirstTurn);
                sensor.AddObservation(card.isAttacked);
                sensor.AddObservation(card.isAssert);
                sensor.AddObservation(card.Assert);
                sensor.AddObservation(card.attackTimes);
                sensor.AddObservation(CardTypeInt(card.Type));
            }
            else
            {
                sensor.AddObservation(-1);
                sensor.AddObservation(-1);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
            }
        }
    }

    private int CardTypeInt(Card.CardType type)
    {
        switch(type)
        {
            case Card.CardType.Object:
                return 0;
            case Card.CardType.Method:
                return 1;
            case Card.CardType.Scope:
                return 2;
            default:
                return -1;
        }
    }

    // Discrete Branch構成 (合計28branch。BehaviorParametersのInspectorで以下のサイズを設定する必要があります)
    //   [5, 8, 20, 20,
    //    2,2,2,2,                                          // 手札マスク x4 (Mariganは初手4枚固定のため)
    //    2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2]          // 場マスク x20
    //
    //   Branch 0     (5) : ActionType  0=Marigan, 1=SelfGarbage, 2=Play, 3=Attack, 4=End
    //   Branch 1     (8) : 手札インデックス        (Play の対象選択に使用)
    //   Branch 2     (20): 自分の場インデックス     (Attack の攻撃元 / Play の対象が selfField の場合)
    //   Branch 3     (20): 相手の場インデックス     (Attack の対象 / Play の対象が enemyField の場合)
    //   Branch 4~7   (2 x4) : 手札マスク  [i]=1なら手札のi番目をMariganの対象に含める(初手4枚固定)
    //   Branch 8~27  (2 x20): 場マスク    [i]=1なら自分の場のi番目をSelfGarbageの対象に含める
    public override void OnActionReceived(ActionBuffers actions)
    {
        var d = actions.DiscreteActions;

        int actionTypeIndex = d[0];
        int handIndex       = d[1];
        int myFieldIndex    = d[2];
        int enemyFieldIndex = d[3];

        ActionType actionType = (ActionType)actionTypeIndex;
        PlayerAction playerAction = null;

        switch (actionType)
        {
            case ActionType.Marigan:
            {
                // 手札マスク(Branch4~7)を見て、引き直したいカードを複数選択する
                // Mariganは初手ドロー直後(手札4枚固定)にしか発生しないため4枠で足りる
                List<Card> target = new List<Card>();
                for (int i = 0; i < myPlayer.hand.Count && i < 4; i++)
                {
                    if (d[4 + i] == 1)
                    {
                        target.Add(myPlayer.hand[i]);
                    }
                }
                // targetが0枚でも「マリガンしない」という有効な選択として扱う
                playerAction = new PlayerAction(ActionType.Marigan, target);
                break;
            }

            case ActionType.SelfGarbage:
            {
                // 場マスク(Branch8~27)を見て、破棄したい自分の場のカードを複数選択する
                List<Card> target = new List<Card>();
                for (int i = 0; i < myPlayer.field.Count && i < 20; i++)
                {
                    if (d[8 + i] == 1)
                    {
                        target.Add(myPlayer.field[i]);
                    }
                }
                playerAction = new PlayerAction(ActionType.SelfGarbage, target);
                break;
            }

            case ActionType.Play:
            {
                // 手札からカードをプレイする(対象は単一のためBranch1をそのまま使用)
                if (handIndex < myPlayer.hand.Count)
                {
                    Card sourceCard = myPlayer.hand[handIndex];
                    List<Card> target = null;

                    // カードごとの Select 情報にあわせて対象を決定する
                    if (sourceCard.select != null && sourceCard.select.isSelectConstructor)
                    {
                        List<Card> pool = null;
                        int targetIndex = -1;

                        switch (sourceCard.select.whereTarget)
                        {
                            case where.hand:
                                pool = myPlayer.hand;
                                targetIndex = handIndex;
                                break;
                            case where.selfField:
                                pool = myPlayer.field;
                                targetIndex = myFieldIndex;
                                break;
                            case where.enemyField:
                                pool = enemyPlayer.field;
                                targetIndex = enemyFieldIndex;
                                break;
                        }

                        if (pool != null && targetIndex >= 0 && targetIndex < pool.Count)
                        {
                            target = new List<Card>() { pool[targetIndex] };
                        }
                    }

                    // targetがnullの場合、4引数コンストラクタはAddRange(null)で例外になるため
                    // ターゲット不要な2引数コンストラクタに分岐する
                    playerAction = (target != null)
                        ? new PlayerAction(ActionType.Play, sourceCard, target)
                        : new PlayerAction(ActionType.Play, sourceCard);
                }
                break;
            }

            case ActionType.Attack:
            {
                // 自分の場のカードで攻撃する(対象は単一のためBranch2/3をそのまま使用)
                if (myFieldIndex < myPlayer.field.Count)
                {
                    Card sourceCard = myPlayer.field[myFieldIndex];
                    List<Card> target = null;

                    // 相手の場にカードがあれば対象として指定。いなければ直接攻撃(target=null)。
                    if (enemyPlayer.field.Count > 0 && enemyFieldIndex < enemyPlayer.field.Count)
                    {
                        target = new List<Card>() { enemyPlayer.field[enemyFieldIndex] };
                    }

                    playerAction = (target != null)
                        ? new PlayerAction(ActionType.Attack, sourceCard, target)
                        : new PlayerAction(ActionType.Attack, sourceCard);
                }
                break;
            }

            case ActionType.End:
            {
                playerAction = new PlayerAction(ActionType.End);
                break;
            }
        }

        // インデックスが不正で行動を組み立てられなかった場合
        if (playerAction == null)
        {
            AddReward(-0.05f);
            gm.decisionTick++;
            return;
        }

        bool isCorrect = gm.ExecuteAction(myPlayer, enemyPlayer, playerAction);

        Debug.Log($"【Action】{gameObject.name} type:{actionType} isCorrect:{isCorrect}"); // ←追加log用

        if (!isCorrect)
        {
            // ルール上実行できない行動を選んだ場合のペナルティ
            AddReward(-0.05f);
        }
        else
        {
            // 行動が成立したことに対する小さな報酬
            AddReward(0.01f);
        }

        // 勝敗による最終報酬(+1 / -1)は GameManager.OnGameFinished イベント側で
        // AddReward() と EndEpisode() を呼ぶ設計を想定しています。
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        for (int i = 0; i < d.Length; i++) d[i] = 0;

        // フェーズに応じたデフォルト行動を決める(キー未入力時の安全策)
        int actionType;
        if (gm.currentPhase == PhaseState.Start)
        {
            // 初手マリガン中はMarigan、それ以降のStart(自壊フェーズ)はSelfGarbage
            actionType = (gm.systemTurn == 1) ? 0 : 1;
        }
        else
        {
            // メインフェーズはEndをデフォルトにする(無難にターンを終える)
            actionType = 4;
        }

        // 1:Marigan 2:SelfGarbage 3:Play 4:Attack 5:End
        if (Keyboard.current.digit1Key.isPressed) actionType = 0;
        else if (Keyboard.current.digit2Key.isPressed) actionType = 1;
        else if (Keyboard.current.digit3Key.isPressed) actionType = 2;
        else if (Keyboard.current.digit4Key.isPressed) actionType = 3;
        else if (Keyboard.current.digit5Key.isPressed) actionType = 4;
        d[0] = actionType;

        int handIndex = 0;
        if (Keyboard.current.wKey.isPressed) handIndex = 1;
        else if (Keyboard.current.eKey.isPressed) handIndex = 2;
        else if (Keyboard.current.rKey.isPressed) handIndex = 3;
        d[1] = handIndex;

        d[2] = 0;
        d[3] = 0;
        Debug.Log($"【Heuristic】{gameObject.name} actionType:{actionType} handIndex:{handIndex}");
    }

}