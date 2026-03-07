using UnityEngine;

/// <summary>
/// Placeable station. Employee brings cooked patty + ingredients here to "assemble" the order.
/// </summary>
public class AssemblyStation : MonoBehaviour
{
    public Vector3 interactionOffset = Vector3.zero;

    public Vector3 GetInteractionPosition()
    {
        return transform.position + interactionOffset;
    }
}
