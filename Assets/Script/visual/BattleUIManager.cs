using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BattleUIManager : MonoBehaviour
{
    [Header("System")]
    public BattleManager battleManager;
    public PlayerInputManager inputManager;
    public Button endTurnButton;
    public TextMeshProUGUI systemText;

    [Header("self UI")]
    public TextMeshProUGUI selfDataText; 
    public TextMeshProUGUI selfusedMemory; //１ターンですでに使ったメモリ
    public TextMeshProUGUI selfusableMemory; //1ターン中に使用可能なメモリ
    public TextMeshProUGUI selffieldMemory; //使用済みメモリ
    public TextMeshProUGUI selfmaxMemory; //最大メモリ

    [Header("Enemy UI")]
    public TextMeshProUGUI enemyDataText; 
    public TextMeshProUGUI enemyusedMemory; //１ターンですでに使ったメモリ
    public TextMeshProUGUI enemyusableMemory; //1ターン中に使用可能なメモリ
    public TextMeshProUGUI enemyfieldMemory; //使用済みメモリ
    public TextMeshProUGUI enemymaxMemory; //最大メモリ
    [Header("DrawField")] 
    public GameObject witchPlay;
    public GameObject SelectCard;
    public GameObject MariganField;
    public Button mariganConfirmButton;
    [Header("Card DBS")]
    public CardConect cardDatabase;
    [Header("Card PopUp")]
    public GameObject cardPopupPanel;
    public TextMeshProUGUI cardPopupText;
    [Header("End Game")]
    public GameObject endGame;
    public TextMeshProUGUI endText;
    private void Start()
    {
        if (mariganConfirmButton != null) 
        {
            mariganConfirmButton.onClick.AddListener(() => DecideMarigan());
        }
        if(endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(()=>OnEndTurnClicked());
        }
    }
    private void GameEnd(bool win)
    {
        endGame.SetActive(true);
        if(win)
        {
            endText.text = "勝利";
        }
        else
        {
            endText.text = "敗北";
        }
    }
    private void DecideMarigan()
    {
        mariganConfirmButton.gameObject.SetActive(false);
        inputManager.ConfirmMarigan();
    }
    
    public void ShowMarigan()
    {
        if (MariganField != null) MariganField.SetActive(true);
        if (mariganConfirmButton != null) mariganConfirmButton.gameObject.SetActive(true);
    }

    public void HideMarigan()
    {
        if (MariganField != null) MariganField.SetActive(false);
        if (mariganConfirmButton != null) mariganConfirmButton.gameObject.SetActive(false);
    }
    public void OnEndTurnClicked()
    {
        battleManager.SubmitEndTurn();
    }
    public void UpdateUI(GameManager gm,Player self,Player enemy)
    {
        systemText.text = (gm.turn == self ? "Turn: You" : "Turn:") + " " + $" Phase: {gm.currentPhase}";
        selfDataText.text = $"Hand:{self.hand.Count} Deck:{self.deck.Count} Garbage:{self.garbage.Count}";
        enemyDataText.text = $"Hand:{enemy.hand.Count} Deck:{enemy.deck.Count} Garbage:{enemy.garbage.Count}";
        selffieldMemory.text = $"{self.fieldCost}";
        selfmaxMemory.text = $"{self.maxMemory}";
        selfusableMemory.text = $"{self.usableMemory}";
        selfusedMemory.text = $"{self.usedMemory}";
        enemyfieldMemory.text = $"{enemy.fieldCost}";
        enemymaxMemory.text = $"{enemy.maxMemory}";
        enemyusableMemory.text = $"{enemy.usableMemory}";
        enemyusedMemory.text = $"{enemy.usedMemory}";
    }
    public void ShowPopUp(string abilityText)
    {
        if (cardPopupPanel == null || cardPopupText == null) return;
        cardPopupText.text = abilityText;
        cardPopupPanel.transform.SetAsLastSibling();
        cardPopupPanel.SetActive(true);
    }
    public void HidePopUp()
    {
        if (cardPopupPanel != null) cardPopupPanel.SetActive(false);
    }

    public void UpdateNetworkUI(bool myTurn, PhaseState phase, int[] self, int[] enemy)
    {
        systemText.text = (myTurn ? "あなたのターン" : "相手のターン") + $"\nフェーズ: {phase}";
        selfDataText.text = $"Hand:{self[0]} Deck:{self[6]} Garbage:{self[1]}";
        enemyDataText.text = $"Hand:{enemy[0]} Deck:{enemy[6]} Garbage:{enemy[1]}";
        selfmaxMemory.text = self[2].ToString();
        selffieldMemory.text = self[3].ToString();
        selfusableMemory.text = self[4].ToString();
        selfusedMemory.text = self[5].ToString();
        enemymaxMemory.text = enemy[2].ToString();
        enemyfieldMemory.text = enemy[3].ToString();
        enemyusableMemory.text = enemy[4].ToString();
        enemyusedMemory.text = enemy[5].ToString();
    }

    public Button CreateActionButton(string label, Transform parent, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        Button button = Instantiate(endTurnButton, parent);
        button.name = label;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        button.interactable = true;
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(240, 55);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null) { text.text = label; text.fontSize = 22; }
        button.gameObject.SetActive(true);
        return button;
    }
}
