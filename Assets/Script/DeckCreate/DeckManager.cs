using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DeckManager
{
    public static List<string> player1Deck{get;private set;} = new List<string>();
    public static List<string> player2Deck{get;private set;}= new List<string>();
    public const int MAXDECKNUM = 40;
    public const int MAXSAMECARD = 4;
    public static void AddDeck(int wicthDeck,string className)
    {
        switch (wicthDeck)
        {
            case 1:
                if(player1Deck.Count >= MAXDECKNUM) break;
                if(player1Deck.Count(f=>f==className) < MAXSAMECARD)player1Deck.Add(className);
                break;
            case 2:
                if(player2Deck.Count >= MAXDECKNUM) break;
                if(player2Deck.Count(f=>f==className) < MAXSAMECARD)player2Deck.Add(className);
                break;
        }
        SaveDeck();
    }
    public static void RemoveDeck(int wicthDeck,string className)
    {
        switch (wicthDeck)
        {
            case 1:
                player1Deck.Remove(className);
                break;
            case 2:
                player2Deck.Remove(className);
                break;
        }
        SaveDeck();
    }
    public static void SaveDeck()
    {
        DeckData data1 = new DeckData();
        data1.deck = player1Deck;
        string json1 = JsonUtility.ToJson(data1);
        PlayerPrefs.SetString("Player1DeckSave", json1);

        DeckData data2 = new DeckData();
        data2.deck = player2Deck;
        string json2 = JsonUtility.ToJson(data2);
        PlayerPrefs.SetString("Player2DeckSave", json2);

        PlayerPrefs.Save();
        Debug.Log("デッキをオートセーブしました");
    }
    public static void LoadDeck()
    {
        if (PlayerPrefs.HasKey("Player1DeckSave"))
        {
            string json1 = PlayerPrefs.GetString("Player1DeckSave");
            DeckData data1 = JsonUtility.FromJson<DeckData>(json1);
            player1Deck = data1.deck;
        }

        if (PlayerPrefs.HasKey("Player2DeckSave"))
        {
            string json2 = PlayerPrefs.GetString("Player2DeckSave");
            DeckData data2 = JsonUtility.FromJson<DeckData>(json2);
            player2Deck = data2.deck;
        }
        Debug.Log("保存されたデッキをロードしました");
    }
    
    public static Card CreateCardInstance(string className)
    {
        switch (className)
        {
            case "SledOverClock": return new SledOverClock();
            case "IncrementProcess": return new IncrementProcess();
            case "ClockDownBot": return new ClockDownBot();
            case "ParallelCompilation": return new ParallelCompilation();
            case "PoisonPoint": return new PoisonPoint();
            case "UnSafeArea": return new UnSafeArea();
            case "Master": return new Master();
            case "Raid10": return new Raid10();
            case "RmRf": return new RmRf();
            case "Paging": return new Paging();
            case "BackGroundMiner": return new BackGroundMiner();
            case "SystemFreeze": return new SystemFreeze();
            case "CarnelPanicZero": return new CarnelPanicZero();
            case "AllDelete": return new AllDelete();
            default:
                Debug.LogError($"未定義のカードクラス名です: {className}");
                return null;
        }
    }
    public static List<Card> CreateBasicCardDeck()
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