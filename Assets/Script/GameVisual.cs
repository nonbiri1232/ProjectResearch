using UnityEngine;
using UnityEngine.UI; // Buttonを使うために必要です
using TMPro; // 【追加】TextMeshProを使うための宣言
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine.Assemblies;
using Unity.VisualScripting;
using UnityEngine.Timeline;

public class GameVisual : MonoBehaviour
{
    [Header("System")]
    public Button endTurnButton;
    
    public Button selfGarbageButton;
    public TextMeshProUGUI systemText;

    [Header("Player 1 UI")]
    public TextMeshProUGUI p1MemoryText;
    public Transform p1HandArea;
    public Transform p1FieldArea;

    [Header("Player 2 UI")]
    public TextMeshProUGUI p2MemoryText;
    public Transform p2HandArea;
    public Transform p2FieldArea;

    [Header("Prefabs")]
    public GameObject cardButtonPrefab; 
    public GameObject witchPlay;
    public GameObject witchPlaySelect;
    public GameObject SelectCard;

    private GameManager gm;
    private Player player1;
    private Player player2;
    private Card selectedPlayCard;
    private Card selectedAttackCard;
    List<Card> selectList = new List<Card>();
    private bool isCostAdd;
    public void isAdd(bool cost)
    {
        isCostAdd = cost;
    }
    public void OpenSelectCard()
    {
        SelectCard.SetActive(true);
    }

    List<Card> deck = new List<Card>(){
        new SledOverClock(),new IncrementProcess(),new ClockDownBot(),new ParallelCompilation(),
        new PoisonPoint(),new UnSafeArea(),new Master(),new Raid10(),new RmRf(),new Paging(),new BackGroundMiner(),
        new SystemFreeze(),new CarnelPanicZero(),new AllDelete()
    }; 
    void Start()
    {
        player1 = new Player(CreateBasicCardDeck());
        player2 = new Player(CreateBasicCardDeck());

        gm = new GameManager(player1, player2);

        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        selfGarbageButton.onClick.AddListener(OnSelfGarbageClicked);

        UpdateUI();
    }

    public void OnEndTurnClicked()
    {
        PlayerAction action = new PlayerAction();
        action.type = ActionType.End;

        gm.ExecuteAction(gm.turn, GetEnemyPlayer(), action); 
        UpdateUI();
    }
    private void UpdateUI()
    {
        systemText.text = (gm.turn == player1 ? "Turn: Player 1" : "Turn: Player 2") + " " + $" Phase: {gm.currentPhase}";
        p1MemoryText.text = $"field/maxMemory: {player1.fieldCost} / {player1.maxMemory} \nused/usable: {player1.usedMemory} / {player1.usableMemory} \ndeckNum {player1.deck.Count}\n garbageNum {player1.garbage.Count}";
        p2MemoryText.text = $"field/maxMemory: {player2.fieldCost} / {player2.maxMemory} \nused/usable: {player2.usedMemory} / {player2.usableMemory} \ndeckNum {player2.deck.Count}\n garbageNum {player2.garbage.Count}";

        DrawHand(player1, p1HandArea);
        DrawHand(player2, p2HandArea);
        DrawField(player1, p1FieldArea);
        DrawField(player2, p2FieldArea);
    }

    private void DrawHand(Player targetPlayer, Transform handArea)
    {
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        foreach (Card c in targetPlayer.hand)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, handArea);
            
            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>OnClickCardHand(c,c.select));
        }
    }


    private void DrawField(Player targetPlayer, Transform fieldArea)
    {
        foreach(Transform child in fieldArea)
        {
            Destroy(child.gameObject);
        }
        foreach(Card c in targetPlayer.field)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, fieldArea);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>OnClickCardField(c));
        }
    }
    private void DrawSelectCard(Select select)
    {
        Transform selectArea = SelectCard.GetComponent<Transform>();
        foreach(Transform child in selectArea)
        {
            Destroy(child.gameObject);
        }
        List<Card> field;
        if (select.whereTarget == where.selfField)
        {
            
            field = gm.turn.field;
        }
        else if(select.whereTarget == where.enemyField)
        {
            field = GetEnemyPlayer().field;
        }
        else
        {
            field = gm.turn.hand;
        }
        foreach(Card c in field)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, selectArea);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>OnclickSelect(select,c));
        }
    }
    private int selectNum;
    private void OnClickCardHand(Card c,Select select)
    {
        if(gm.currentPhase != PhaseState.Main) return;
        if(c.player == gm.turn && c.Cost <= c.player.maxMemory - c.player.fieldCost && c.Cost <= c.player.usableMemory - c.player.usedMemory)
        {
            selectedPlayCard = c;
            if(c.Type == Card.CardType.Object)
            {
                if(select.whereTarget == where.hand)
                {
                    DrawSelectCard(select);
                    if(c.Cost + 1 <= c.player.maxMemory - c.player.fieldCost && c.Cost + 1 <= c.player.usableMemory - c.player.usedMemory)
                        witchPlaySelect.SetActive(true);
                    else{
                        SelectCard.SetActive(true);
                        isCostAdd = false;
                    }
                }
                else if (select.isSelectConstructor && IsSelf(select).field.Count > 0)
                {
                    DrawSelectCard(select);
                    if(c.Cost + 1 <= c.player.maxMemory - c.player.fieldCost && c.Cost + 1 <= c.player.usableMemory - c.player.usedMemory)
                        witchPlaySelect.SetActive(true);
                    else{
                        SelectCard.SetActive(true);
                        isCostAdd = false;
                    }
                }
                else
                {    
                    if(c.Cost + 1 <= c.player.maxMemory - c.player.fieldCost && c.Cost + 1 <= c.player.usableMemory - c.player.usedMemory)
                        witchPlay.SetActive(true);
                    else
                        PlayAction(false);
                }
            }
            else if(c.Type == Card.CardType.Method)
            {
                if(select.whereTarget == where.hand)
                {
                    SelectCard.SetActive(true);
                    DrawSelectCard(select);
                }
                else if (select.isSelectConstructor && IsSelf(select).field.Count > 0)
                {
                    SelectCard.SetActive(true);
                    DrawSelectCard(select);
                }
                else
                {
                    PlayAction(false);
                }
            }
            else
            {
                PlayAction(false);
            }
            
        }
    }
    private Player IsSelf(Select select)
    {
        if(select.whereTarget == where.selfField)return gm.turn;
        else return GetEnemyPlayer();
    }
    
    
    private void OnclickSelect(Select select,Card c)
    {
        selectNum++;
        selectList.Add(c);
        if(selectNum >= select.numOfSelect)
        {
            selectNum = 0;
            List<Card> finalTargets = new List<Card>(selectList);
            selectList.Clear();
            PlayActionSelect(isCostAdd,finalTargets);
            
        }
    }

    public void PlayActionSelect(bool isAddCost,List<Card> cards)
    {
        
        SelectCard.SetActive(false);
        var action = new PlayerAction(ActionType.Play,selectedPlayCard,cards);
        action.isAddCost = isAddCost;
        if (isAddCost)
        {
            Debug.Log($"＋１コストでプレイします。");
        }
        isCostAdd = false;
        selectedPlayCard = null;
        bool isCorrect;
        isCorrect = gm.ExecuteAction(gm.turn,GetEnemyPlayer(),action);

        if (isCorrect)
        {
            UpdateUI();
        }
    }
    public void PlayAction(bool isAddCost)
    {
        var action = new PlayerAction(ActionType.Play,selectedPlayCard);
        action.isAddCost = isAddCost;
        if (isAddCost)
        {
            Debug.Log($"＋１コストでプレイします。");
        }
        selectedPlayCard = null;
        bool isCorrect;
        isCorrect = gm.ExecuteAction(gm.turn,GetEnemyPlayer(),action);

        if (isCorrect)
        {
            UpdateUI();
        }
    }
    public void CancelPlay()
    {
        selectedPlayCard = null;

        UpdateUI();
    }
    
    private void OnClickCardField(Card c)
    {
        if(gm.currentPhase != PhaseState.Main) return;
        selectedAttackCard = c;
        if(c.player == gm.turn && (c.isImmediate || !c.isFirstTurn) && c.isCanAttack)
        {
            SelectCard.SetActive(true);
            if(GetEnemyPlayer().field.Count > 0)
            {
                DrawSelectCard();
            }
            else
            {
                DirectAttack(c);
            }
        }
    }
    private void AttackAction(Card c)
    {
        SelectCard.SetActive(false);
        PlayerAction action;
        if(c != null)
        {
            List<Card> target = new List<Card>{c};
            action = new PlayerAction(ActionType.Attack,selectedAttackCard,target);
        }
        else
        {
            action = new PlayerAction(ActionType.Attack,selectedAttackCard);
        }
        selectedAttackCard = null;
        bool isCorrect;
        isCorrect = gm.ExecuteAction(gm.turn,GetEnemyPlayer(),action);

        if (isCorrect)
        {
            Debug.Log($"攻撃処理が正常に処理されました。");
            UpdateUI();
            return;
        }
        Debug.Log($"何らかの要因によって攻撃処理が失敗しました。");
    }
    private void DrawSelectCard()
    {
        Transform selectArea = SelectCard.GetComponent<Transform>();
        foreach(Transform child in selectArea)
        {
            Destroy(child.gameObject);
        }
        foreach(Card c in GetEnemyPlayer().field)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, selectArea);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AttackAction(c));
        }
    }
    
    private void DirectAttack(Card c)
    {
        Transform selectArea = SelectCard.GetComponent<Transform>();
        foreach(Transform child in selectArea)
        {
            Destroy(child.gameObject);
        }
        GameObject cardObj = Instantiate(cardButtonPrefab, selectArea);

        TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
        
        btnText.text = $"DirectAttack"; 

        Button btn = cardObj.GetComponent<Button>();

        btn.onClick.AddListener(()=>AttackAction(null));
    }

    public void OnSelfGarbageClicked()
    {
        if(gm.currentPhase != PhaseState.Start) return;
        SelectCard.SetActive(true);
        DrawSelectCardSelfGarbage();
    }
    private void DrawSelectCardSelfGarbage()
    {
        Transform selectArea = SelectCard.GetComponent<Transform>();
        foreach(Transform child in selectArea)
        {
            Destroy(child.gameObject);
        }
        List<Card> field = gm.turn.field;

        GameObject decide = Instantiate(cardButtonPrefab, selectArea);

        TextMeshProUGUI decideText = decide.GetComponentInChildren<TextMeshProUGUI>();
        
        decideText.text = $"Decide"; 

        Button decidebtn = decide.GetComponent<Button>();

        decidebtn.onClick.AddListener(()=>DecideSelfGarbage());
        foreach(Card c in field)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab, selectArea);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{c.GetType().Name}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AddSelfGarbage(c,cardObj));
        }
    }
    List<Card> selfGarbageList = new List<Card>();
    private void AddSelfGarbage(Card c,GameObject obj)
    {
        selfGarbageList.Add(c);
        Destroy(obj);
    }
    private void DecideSelfGarbage()
    {
        SelectCard.SetActive(false);
        var action = new PlayerAction(ActionType.SelfGarbage,selfGarbageList);
        bool isCorrect;
        isCorrect = gm.ExecuteAction(gm.turn,GetEnemyPlayer(),action);
        selfGarbageList.Clear();
        if (isCorrect)
        {
            UpdateUI();
        }
    }

    private Player GetEnemyPlayer()
    {
        return gm.turn == player1 ? player2 : player1;
    }

    private List<Card> CreateBasicCardDeck()
    {
        List<Card> deck = new List<Card>();
        for (int i = 1; i < 11; i++)
        {
            for(int j = 0;j < 4; j++)
            {
                Card c = new Card();
                c.SettingBasicCard(i);
                deck.Add(c);
            }
        }
        return deck;
    }
}