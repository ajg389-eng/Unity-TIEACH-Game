using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeable station. Holds ingredients (cheese, lettuce, tomato, fries, etc.).
/// Employee walks here to grab each requested ingredient.
/// </summary>
public class PantryStation : MonoBehaviour
{
    [Tooltip("Ingredients this pantry stocks (cheese, lettuce, tomato, fries, etc.)")]
    public List<ItemDefinition> stockedItems = new List<ItemDefinition>();
    [Tooltip("Time in seconds for the employee to grab one ingredient.")]
    public float interactionTimeSeconds = 0.5f;
    public Vector3 interactionOffset = Vector3.zero;

    public Vector3 GetInteractionPosition()
    {
        var tiles = GetComponent<StationInteractionTiles>();
        if (tiles != null) return tiles.GetFirstInteractionPosition();
        return transform.position + interactionOffset;
    }

    public bool HasItem(ItemDefinition item)
    {
        return item != null && stockedItems != null && stockedItems.Contains(item);
    }

    public bool TakeItem(ItemDefinition item)
    {
        if (!HasItem(item)) return false;
        return true;
    }
}
