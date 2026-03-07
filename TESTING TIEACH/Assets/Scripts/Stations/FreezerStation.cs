using UnityEngine;

/// <summary>
/// Placeable station. Employee walks here to grab a patty for the burger.
/// Assign patty item in ProductionManager; this station just provides an interaction point.
/// </summary>
public class FreezerStation : MonoBehaviour
{
    public Vector3 interactionOffset = Vector3.zero;

    public Vector3 GetInteractionPosition()
    {
        return transform.position + interactionOffset;
    }
}
