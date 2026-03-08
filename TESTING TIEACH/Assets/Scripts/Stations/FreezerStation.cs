using UnityEngine;

/// <summary>
/// Placeable station. Employee walks here to grab a patty for the burger.
/// Assign patty item in ProductionManager; this station just provides an interaction point.
/// </summary>
public class FreezerStation : MonoBehaviour
{
    [Tooltip("Time in seconds for the employee to grab a patty.")]
    public float interactionTimeSeconds = 1f;
    public Vector3 interactionOffset = Vector3.zero;

    public Vector3 GetInteractionPosition()
    {
        var tiles = GetComponent<StationInteractionTiles>();
        if (tiles != null) return tiles.GetFirstInteractionPosition();
        return transform.position + interactionOffset;
    }
}
