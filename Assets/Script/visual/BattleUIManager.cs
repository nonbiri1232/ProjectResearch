using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BattleUIManager : MonoBehaviour
{
    [Header("System")]
    public BattleManager battleManager;
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
    [Header("Card DBS")]
    public CardConect cardDatabase;
    [Header("Card PopUp")]
    public GameObject cardPopupPanel;
    public TextMeshProUGUI cardPopupText;
    [Header("End Game")]
    public GameObject endGame;
    public TextMeshProUGUI endText;
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
    
    private void ShowMarigan()
    {
        
    }
    private void HideMarigan()
    {
        
    }
    public void OnEndTurnClicked()
    {
        PlayerAction action = new PlayerAction();
        action.type = ActionType.End;
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
        cardPopupText.text = abilityText;
        cardPopupPanel.SetActive(true);
    }
    public void HidePopUp()
    {
        cardPopupPanel.SetActive(false);
    }
}