using UnityEngine;

/// <summary>
/// Placeable station. Employee brings cooked patty + ingredients here to "assemble" the order.
/// </summary>
public class AssemblyStation : MonoBehaviour
{
    [Tooltip("Time in seconds for the employee to assemble the order.")]
    public float interactionTimeSeconds = 1.2f;
    public Vector3 interactionOffset = Vector3.zero;

    public Vector3 GetInteractionPosition()
    {
        var tiles = GetComponent<StationInteractionTiles>();
        if (tiles != null) return tiles.GetFirstInteractionPosition();
        return transform.position + interactionOffset;
    }
}
