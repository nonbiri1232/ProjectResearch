using System.Collections.Generic;
using UnityEngine;

public class LocalBattleVisual : MonoBehaviour
{
    private List<Card> selfHand;
    private List<Card> selfField;
    private List<Card> enemyField;
    private int[] selfMemory = new int[7];
    private int[] enemyMemory = new int[7];
    private bool isMyTurn;
    private Card Scope;
    public void UpdateUI()
    {
        
    }
    public void SetupInitialBoard(List<Card> selfHand,List<Card> selfField,List<Card> enemyField,int[] selfMemory,int[] enemyMemory)
    {
        
    }
}
