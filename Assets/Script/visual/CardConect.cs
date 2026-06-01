using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class CardSetting
{
    public string className;
    public string displayName;
    public string ability;

}
[CreateAssetMenu(fileName = "CardConect", menuName = "Scriptable Objects/CardConect")]
public class CardConect : ScriptableObject
{
    public List<CardSetting> cards = new List<CardSetting>();
}
