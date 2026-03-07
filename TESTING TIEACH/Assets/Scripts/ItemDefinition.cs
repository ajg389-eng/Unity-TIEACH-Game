using UnityEngine;

[CreateAssetMenu(menuName = "FactoryGame/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public GameObject prefab;   // what gets placed in the world
    public int price = 10;
    public int startingQuantity = 0;
}