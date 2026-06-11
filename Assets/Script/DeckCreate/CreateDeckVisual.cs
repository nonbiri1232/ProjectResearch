using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreateDeckVisual : MonoBehaviour
{
    [Header("UI Area")]
    [SerializeField] private Transform cardPoolArea;
    [SerializeField] private Transform myDeckArea;
    [SerializeField] private TextMeshProUGUI deckSizeText;

    [Header("Prefab")]
    [SerializeField] private GameObject cardButtonPrefab;

    [Header("Card DBS")]
    public CardConect cardDatabase;

    [Header("Card PopUp")]
    public GameObject cardPopupPanel;
    public TextMeshProUGUI cardPopupText;
    private int nowChangeDeck = 1;

    private List<string> allAvailableCards = new List<string>()
    {
        "SledOverClock", "IncrementProcess", "ClockDownBot", "ParallelCompilation",
        "PoisonPoint", "UnSafeArea", "Master", "Raid10", "RmRf", "Paging",
        "BackGroundMiner", "SystemFreeze", "CarnelPanicZero", "AllDelete"
    };

    void Start()
    {
        DeckManager.LoadDeck();
        UpdateUI();
    }

    public void SelectChangeDeck(int i)
    {
        Debug.Log($"player{i}に変更しました。");
        nowChangeDeck = i;
        UpdateUI();
    }
    private List<string> ChangeDeck()
    {
        switch (nowChangeDeck)
        {
            case 1:
                return DeckManager.player1Deck;
            case 2:
                return DeckManager.player2Deck;
        }
        return null;
    }
    private void UpdateUI()
    {
        if(nowChangeDeck == 1)
        {
            deckSizeText.text = $"Deck1: {DeckManager.player1Deck.Count} / {DeckManager.MAXDECKNUM}";
        }
        else if(nowChangeDeck == 2)
        {
            deckSizeText.text = $"Deck2: {DeckManager.player2Deck.Count} / {DeckManager.MAXDECKNUM}";
        }
        else
        {
            deckSizeText.text = "";
        }
        foreach(Transform child in cardPoolArea) Destroy(child.gameObject);
        foreach(string cardName in allAvailableCards)
        {
            GameObject cardObj = Instantiate(cardButtonPrefab ,cardPoolArea,false);

            
            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = $"Cost:{GetCardCost(cardName)}\n{GetCardName(cardName)}"; 

            EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
            if(trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data)=>{ShowPopUp(GetCardAbility(cardName));});
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data)=>{HidePopUp();});
            trigger.triggers.Add(entryExit);
            
            Button btn = cardObj.GetComponent<Button>();
            btn.onClick.AddListener(() => AddToDeck(cardName));
        }
        foreach(Transform child in myDeckArea) Destroy(child.gameObject);
        foreach(string cardName in ChangeDeck())
        {
            GameObject cardObj = Instantiate(cardButtonPrefab ,myDeckArea,false);

            
            TextMeshProUGUI btnText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = $"Cost:{GetCardCost(cardName)}\n{GetCardName(cardName)}"; 

            EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
            if(trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data)=>{ShowPopUp(GetCardAbility(cardName));});
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data)=>{HidePopUp();});
            trigger.triggers.Add(entryExit);
            
            Button btn = cardObj.GetComponent<Button>();
            btn.onClick.AddListener(() => RemoveToDeck(cardName));
        }

    }
    private void AddToDeck(string cardName)
    {
        DeckManager.AddDeck(nowChangeDeck,cardName);
        UpdateUI();
    }
    private void RemoveToDeck(string cardName)
    {
        DeckManager.RemoveDeck(nowChangeDeck,cardName);
        UpdateUI();
    }

    private string GetCardName(string className)
    {
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
    private string GetCardAbility(string className)
    {
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
    private int GetCardCost(string className)
    {
        int cost = 0;
        if(cardDatabase != null)
        {
            foreach(CardSetting name in cardDatabase.cards)
            {
                if(name.className == className)
                {
                    cost = name.cost;
                    break;
                }
            }
        }
        return cost;
    }
    private int GetCardAtk(string className)
    {
        int atk = 0;
        if(cardDatabase != null)
        {
            foreach(CardSetting name in cardDatabase.cards)
            {
                if(name.className == className)
                {
                    atk = name.atk;
                    break;
                }
            }
        }
        return atk;
    }
    private int GetCardHp(string className)
    {
        int hp = 0;
        if(cardDatabase != null)
        {
            foreach(CardSetting name in cardDatabase.cards)
            {
                if(name.className == className)
                {
                    hp = name.hp;
                    break;
                }
            }
        }
        return hp;
    }
    public void ShowPopUp(string abilityText)
    {
        cardPopupText.text = abilityText;
    }
    public void HidePopUp()
    {
        cardPopupText.text = null;
    }
}