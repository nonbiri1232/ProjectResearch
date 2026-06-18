using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalBattleVisual : MonoBehaviour
{
    [Header("System")]
    public Button endTurnButton;
    
    public Button selfGarbageButton;
    public TextMeshProUGUI systemText;

    [Header("Self UI")]
    public TextMeshProUGUI p1MemoryText;
    public Transform p1HandArea;
    public Transform p1FieldArea;

    [Header("Enemy UI")]
    public TextMeshProUGUI p2MemoryText;
    public Transform p2FieldArea;

    [Header("Prefabs")]
    public GameObject cardButtonPrefab;
    [Header("DrawField")] 
    public GameObject witchPlay;
    public GameObject witchPlaySelect;
    public GameObject SelectCard;
    public GameObject MariganField;
    public GameObject MariganFieldPlayer1;
    public GameObject ScopeArea;
    [Header("Card DBS")]
    public CardConect cardDatabase;
    [Header("Card PopUp")]
    public GameObject cardPopupPanel;
    public TextMeshProUGUI cardPopupText;
    [Header("LocalBattleManager")]
    public LocalBattleManager battleManager;
    //保持データ
    private List<Card> selfHand;
    private List<Card> selfField;
    private List<Card> enemyField;
    private int[] selfMemory = new int[7];
    /*
    0:手札枚数
    1:墓場枚数
    2:フィールドのメモリ
    3:フィールドで使ったメモリ
    4:使えるメモリ
    5:使ったメモリ
    6:デッキ枚数*/
    private int[] enemyMemory = new int[7];//上と同様
    private bool isMyTurn;
    private Card Scope;
    private bool isMarigan;
    void Start()
    {
        isMarigan = false;
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        //selfGarbageButton.onClick.AddListener(OnSelfGarbageClicked);
    }
    public void UpdateUI()
    {
        if (!isMarigan)
        {
            DrawMarigan();
            isMarigan = true;
        }
        DrawField();
    }
    public void SetupInitialBoard(int[] selfHand,int[] selfField,int[] enemyField,int[] selfMemory,int[] enemyMemory,int currentScope)
    {
        this.selfHand = ChangeCard(selfHand);
        this.selfField =  ChangeCard(selfField);
        this.enemyField = ChangeCard(enemyField);
        this.selfMemory = selfMemory;
        this.enemyMemory = enemyMemory;
        if(currentScope != -1)
            Scope = DeckManager.CreateCardInstance(currentScope);
        UpdateUI();
    }
    //マリガン用関数
    private void DrawMarigan()
    {
        Transform trf1 = MariganFieldPlayer1.GetComponent<Transform>();
        foreach(Transform t in trf1)
        {
            Destroy(t.gameObject);
        }
        //player1のマリガン決定ボタン表示
        GameObject decide1 = Instantiate(cardButtonPrefab,trf1);

        TextMeshProUGUI btnText1 = decide1.GetComponentInChildren<TextMeshProUGUI>();
            
        btnText1.text = $"Decide"; 

        Button btn1 = decide1.GetComponent<Button>();

        btn1.onClick.AddListener(()=>battleManager.DecideMariganRpc(marigan.ToArray()));

        foreach(Card c in selfHand)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab,trf1);

            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            
            btnText.text = $"Cost:{c.Cost}\n{GetCardName(c)}\nATK:{c.Attack} HP:{c.Hp}"; 

            Button btn = cardObj.GetComponent<Button>();

            btn.onClick.AddListener(()=>AddMarigan(LocalBattleManager.GetCardId(c),cardObj));
        }
    }
    List<int> marigan = new List<int>();
    private void AddMarigan(int c,GameObject obj)
    {
    
        Image img = obj.GetComponent<Image>();
        if (marigan.Contains(c))
        {
            marigan.Remove(c);        
            img.color = Color.white;
        }
        else
        {
            marigan.Add(c); 
            img.color = Color.gray;
        }
    }
    public void EndMarigan()
    {
        MariganField.SetActive(false);
    }
    
    
    public void OnEndTurnClicked()
    {
        PlayerAction action = new PlayerAction();
        action.type = ActionType.End;

        //gm.ExecuteAction(gm.turn, GetEnemyPlayer(), action); 
        UpdateUI();
    }
    private void DrawField()
    {
        
    }
    private void Draw()
    {
        
    }
    private static List<Card> ChangeCard(int[] deckData)
    {
        List<Card> deck = new List<Card>();
        foreach(int i in deckData)
        {
            Card c = DeckManager.CreateCardInstance(i);
            deck.Add(c);
        }
        return deck;
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
