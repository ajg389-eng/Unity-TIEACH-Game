using UnityEngine;

/// <summary>
/// Placeable station. Employee places raw patty, waits for cook time, then takes cooked patty.
/// </summary>
public class GrillStation : MonoBehaviour
{
    public float cookTimeSeconds = 4f;
    [Tooltip("Time for employee to place patty on grill.")]
    public float placeTimeSeconds = 0.5f;
    [Tooltip("Extra wait after patty is cooked before employee can take it.")]
    public float waitAfterCookedSeconds = 0.5f;
    [Tooltip("Time for employee to take cooked patty off grill.")]
    public float takeTimeSeconds = 0.5f;
    public Vector3 interactionOffset = Vector3.zero;

    bool hasPatty;
    float cookTimer;

    public Vector3 GetInteractionPosition()
    {
        var tiles = GetComponent<StationInteractionTiles>();
        if (tiles != null) return tiles.GetFirstInteractionPosition();
        return transform.position + interactionOffset;
    }

    public bool CanPlacePatty()
    {
        return !hasPatty;
    }

    public void PlacePatty()
    {
        if (hasPatty) return;
        hasPatty = true;
        cookTimer = 0f;
    }

    public void UpdateCooking(float deltaTime)
    {
        if (hasPatty && cookTimer < cookTimeSeconds)
            cookTimer += deltaTime;
    }

    public bool IsCooked()
    {
        return hasPatty && cookTimer >= cookTimeSeconds;
    }

    public bool TakeCookedPatty()
    {
        if (!hasPatty || cookTimer < cookTimeSeconds) return false;
        hasPatty = false;
        return true;
    }

    void Update()
    {
        UpdateCooking(Time.deltaTime);
    }
}
