using UnityEngine;

/// <summary>
/// Placeable station. Employee places raw patty, waits for cook time, then takes cooked patty.
/// </summary>
public class GrillStation : MonoBehaviour
{
    public float cookTimeSeconds = 4f;
    public Vector3 interactionOffset = Vector3.zero;

    bool hasPatty;
    float cookTimer;

    public Vector3 GetInteractionPosition()
    {
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
