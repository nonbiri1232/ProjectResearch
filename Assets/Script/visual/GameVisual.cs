using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
    [Header("DrawField")] 
    public GameObject witchPlay;
    public GameObject witchPlaySelect;
    public GameObject SelectCard;
    public GameObject MariganField;
    public GameObject MariganFieldPlayer1;
    public GameObject MariganFieldPlayer2;
    public GameObject ScopeArea;
    [Header("Card DBS")]
    public CardConect cardDatabase;
    [Header("Card PopUp")]
    public GameObject cardPopupPanel;
    public TextMeshProUGUI cardPopupText;
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

    List<Card> player1Deck = new List<Card>();
    
    List<Card> player2Deck = new List<Card>();
    void Start()
    {
        foreach(string className in DeckManager.player1Deck)
        {
            player1Deck.Add(DeckManager.CreateCardInstance(className));
        }
        foreach(string className in DeckManager.player2Deck)
        {
            player2Deck.Add(DeckManager.CreateCardInstance(className));
        }

        player1 = new Player(player1Deck);
        player2 = new Player(player2Deck);

        gm = new GameManager(player1, player2);

        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        selfGarbageButton.onClick.AddListener(OnSelfGarbageClicked);

        DrawMarigan(player1,player2);

        UpdateUI();
    }
    private void DrawMarigan(Player pl1,Player pl2)
    {
        Transform trf1 = MariganFieldPlayer1.GetComponent<Transform>();
        Transform trf2 = MariganFieldPlayer2.GetComponent<Transform>();
        foreach(Transform t in trf1)
        {
            Destroy(t.gameObject);
        }
        //player1のマリガン決定ボタン表示
        GameObject decide1 = Instantiate(cardButtonPrefab,trf1);

        TextMeshProUGUI btnText1 = decide1.GetComponentInChildren<TextMeshProUGUI>();
            
        btnText1.text = $"Decide"; 

        Button btn1 = decide1.GetComponent<Button>();

        btn1.onClick.AddListener(()=>DecideMarigan(pl1,decide1));
        
        //player2のマリガン決定ボタン表示
        GameObject decide2 = Instantiate(cardButtonPrefab,trf2);

        TextMeshProUGUI btnText2 = decide2.GetComponentInChildren<TextMeshProUGUI>();
            
        btnText2.text = $"Decide"; 

        Button btn2 = decide2.GetComponent<Button>();

        btn2.onClick.AddListener(()=>DecideMarigan(pl2,decide2));

        foreach(Card c in pl1.hand)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab,trf1);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AddMarigan(c,cardObj));
        }
        foreach(Card c in pl2.hand)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab,trf2);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AddMarigan(c,cardObj));
        }
    }
    List<Card> marigan1 = new List<Card>();
    List<Card> marigan2 = new List<Card>();
    private void AddMarigan(Card c,GameObject obj)
    {
    
        Image img = obj.GetComponent<Image>();
        if(c.player == player1)
        {
            if (marigan1.Contains(c))
            {
                marigan1.Remove(c);        
                img.color = Color.white;
            }
            else
            {
                marigan1.Add(c); 
                img.color = Color.gray;
            }
        }
        else
        {
            if (marigan2.Contains(c))
            {
                marigan2.Remove(c);        
                img.color = Color.white;
            }
            else
            {
                marigan2.Add(c); 
                img.color = Color.gray;
            }
        }
    }
    int decidePlayer = 0;
    private void DecideMarigan(Player pl,GameObject obj)
    {
        decidePlayer++;
        Destroy(obj);
        MariganAction();
    }
    private void MariganAction()
    {
        if(decidePlayer == 2)
        {
            PlayerAction action1 = new PlayerAction(ActionType.Marigan,marigan1);
            PlayerAction action2 = new PlayerAction(ActionType.Marigan,marigan2);
            Debug.Log($"{gm.systemTurn}が今のターン数");
            bool isCorrect1 = gm.ExecuteAction(player1,player2,action1);
            bool isCorrect2 = gm.ExecuteAction(player2,player1,action2);
            
            Debug.Log($"isCorrect1={isCorrect1} isCorrect2={isCorrect2}");
            if (isCorrect1 && isCorrect2)
            {
                UpdateUI();
                Debug.Log($"画面をアップデートします");
            }
            MariganField.SetActive(false);
        }
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
        DrawScope();
    }
    private void DrawScope()
    {
        if(gm.currentScope == null)
        {
            return;
        }
        Transform trs = ScopeArea.GetComponent<Transform>();
        foreach (Transform child in trs)
        {
            Destroy(child.gameObject);
        }
        Card c = gm.currentScope;
        GameObject cardObj = Instantiate(cardButtonPrefab, trs);
        TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
        btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}"; 

        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if(trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data)=>{ShowPopUp(GetCardAbility(c));});
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data)=>{HidePopUp();});
        trigger.triggers.Add(entryExit);
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
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}"; 

            EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
            if(trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data)=>{ShowPopUp(GetCardAbility(c));});
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data)=>{HidePopUp();});
            trigger.triggers.Add(entryExit);

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
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

            EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
            if(trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data)=>{ShowPopUp(GetCardAbility(c));});
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data)=>{HidePopUp();});
            trigger.triggers.Add(entryExit);

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
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

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
                Debug.Log($"オブジェクトがプレイされました。");
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
                Debug.Log($"メソッドがプレイされました。");
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
            else if(c.Type == Card.CardType.Scope)
            {
                Debug.Log($"スコープがプレイされました。");
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
            Debug.Log($"正常にカードがプレイされました。");
            UpdateUI();
        }
        else
        {
            Debug.Log($"カードプレイが何らかの要因で失敗しました。");
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
            Debug.Log($"正常にカードがプレイされました。");            
            UpdateUI();
        }
        else
        {
            Debug.Log($"カードプレイが何らかの要因で失敗しました。");
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
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

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
        if(gm.turn.field.Count == 0)
        {
            DecideSelfGarbage();
            return;
        }
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
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AddSelfGarbage(c,cardObj));
        }
    }
    List<Card> selfGarbageList = new List<Card>();
    private void AddSelfGarbage(Card c,GameObject obj)
    {
        Image img = obj.GetComponent<Image>();
        if (selfGarbageList.Contains(c))
        {
            selfGarbageList.Remove(c);        
            img.color = Color.white;
        }
        else
        {
            selfGarbageList.Add(c); 
            img.color = Color.gray;
        }
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
            Debug.Log($"セルフガベージが実行されました。");
            UpdateUI();
        }
    }

    private Player GetEnemyPlayer()
    {
        return gm.turn == player1 ? player2 : player1;
    }

    private string GetCardName(Card c)
    {
        string className = c.GetType().Name;
        string displayName = className;
        if(cardDatabase != null)
        {
            foreach(CardSetting name in cardDatabase.cards)
            {
                if(name.className == className)
                {
                    displayName = name.displayName;
                    break;
                }
            }
        }
        return displayName;
    }
    private string GetCardAbility(Card c)
    {
        string className = c.GetType().Name;
        string ability = "なし";
        if(cardDatabase != null)
        {
            foreach(CardSetting name in cardDatabase.cards)
            {
                if(name.className == className)
                {
                    ability = name.ability;
                    break;
                }
            }
        }
        return ability;
    }
    public void ShowPopUp(string abilityText)
    {
        cardPopupText.text = abilityText;
        cardPopupPanel.SetActive(true);
    }
    public void HidePopUp()
    {
        cardPopupPanel.SetActive(false);
    }
}