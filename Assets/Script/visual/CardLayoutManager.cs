using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum FieldType
{
    Hand,
    Field
}

class CardLayoutManager : MonoBehaviour
{
    [SerializeField]private GameObject cardPrefab;
    [SerializeField] private CardConect cardDB;

    [SerializeField]private Vector2[] drawField = new Vector2[2];

    private int drawNum;
    private Vector3 centerPos;
    private float cardX;
    private float cardY;
    private List<CardData> cards = new List<CardData>();
    CardLayoutManager()
    {
        if(drawField[0] == null||drawField[1] == null)
        {
            return;
        }
        float xClamp;
        float yClamp;
        xClamp = drawField[0].x - drawField[1].x;
        yClamp = drawField[0].y - drawField[1].y;
        
        centerPos = new Vector3(xClamp/2,yClamp/2,0);
    }
    private void add(CardData card)
    {
        cards.Add(card);
        drawNum++;
    }
    private void add(CardData[] cards)
    {
        cards.AddRange(cards);
        drawNum += cards.Length;
    }

    public void DrawCard()
    {
        foreach(CardData c in cards)
        {
            
        }
    }

}