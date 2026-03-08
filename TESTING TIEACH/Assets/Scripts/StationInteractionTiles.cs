using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines interaction tiles for the station (where the employee must stand).
/// Assign the highlight object that is part of your station model; it will be shown in Build mode and hidden in Play mode.
/// </summary>
[RequireComponent(typeof(BuildFootprint))]
public class StationInteractionTiles : MonoBehaviour
{
    public GridManager grid;
    public GameModeManager modeManager;

    [Tooltip("Grid offsets from the station footprint origin (bottom-left). (0, -1) = one tile in front; add (1,-1) for 2-wide stations.")]
    public List<Vector2Int> interactionTileOffsets = new List<Vector2Int> { new Vector2Int(0, -1) };

    [Header("Build mode highlight (part of station model)")]
    [Tooltip("The child GameObject(s) in your station model that show the green interaction tiles. Shown in Build mode, hidden in Play mode. Assign one parent that contains all highlight meshes (each child = one quad center).")]
    public GameObject buildModeHighlight;
    [Tooltip("Employee must stand within this distance (XZ) of a quad center to use the station. Uses grid cell size if 0.")]
    public float interactionRadius = 0f;

    BuildFootprint footprint;
    bool lastBuildMode;

    void Awake()
    {
        footprint = GetComponent<BuildFootprint>();
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (modeManager == null) modeManager = FindObjectOfType<GameModeManager>();
        if (interactionTileOffsets == null || interactionTileOffsets.Count == 0)
            interactionTileOffsets = new List<Vector2Int> { new Vector2Int(0, -1) };
    }

    void Update()
    {
        if (modeManager == null) return;
        bool buildMode = modeManager.CurrentMode == GameModeManager.Mode.Build;
        if (buildMode == lastBuildMode) return;
        lastBuildMode = buildMode;

        if (buildModeHighlight != null)
            buildModeHighlight.SetActive(buildMode);
    }

    void OnEnable()
    {
        if (modeManager != null && buildModeHighlight != null)
            buildModeHighlight.SetActive(modeManager.CurrentMode == GameModeManager.Mode.Build);
    }

    /// <summary>Footprint origin cell (bottom-left of the station).</summary>
    public void GetOriginCell(out int originX, out int originY)
    {
        originX = 0;
        originY = 0;
        if (grid == null) return;

        int sizeX = footprint != null ? Mathf.Max(1, footprint.sizeX) : 1;
        int sizeY = footprint != null ? Mathf.Max(1, footprint.sizeY) : 1;
        float continuousX = (transform.position.x - grid.Origin.x) / grid.cellSize;
        float continuousZ = (transform.position.z - grid.Origin.z) / grid.cellSize;
        originX = Mathf.RoundToInt(continuousX - sizeX * 0.5f);
        originY = Mathf.RoundToInt(continuousZ - sizeY * 0.5f);
    }

    /// <summary>World positions (cell centers) for each interaction tile.</summary>
    public List<Vector3> GetInteractionTileWorldPositions()
    {
        var list = new List<Vector3>();
        if (grid == null || interactionTileOffsets == null) return list;

        GetOriginCell(out int ox, out int oy);
        foreach (var offset in interactionTileOffsets)
        {
            int gx = ox + offset.x;
            int gy = oy + offset.y;
            if (gx >= 0 && gx < grid.Width && gy >= 0 && gy < grid.Height)
                list.Add(grid.CellToWorld(gx, gy));
        }
        return list;
    }

    /// <summary>World positions from the highlight quads (center of each child, or the single highlight). Used so worker must stand in the middle of the quad.</summary>
    public List<Vector3> GetInteractionPositionsFromModel()
    {
        var list = new List<Vector3>();
        if (buildModeHighlight == null) return list;
        int childCount = buildModeHighlight.transform.childCount;
        if (childCount > 0)
        {
            for (int i = 0; i < childCount; i++)
                list.Add(buildModeHighlight.transform.GetChild(i).position);
        }
        else
        {
            list.Add(buildModeHighlight.transform.position);
        }
        return list;
    }

    /// <summary>First interaction position (middle of first quad, or grid fallback). Snaps to a walkable cell so pathfinding can reach it.</summary>
    public Vector3 GetFirstInteractionPosition()
    {
        var fromModel = GetInteractionPositionsFromModel();
        if (fromModel != null && fromModel.Count > 0)
        {
            Vector3 quadCenter = fromModel[0];
            if (grid != null && grid.WorldToCell(quadCenter, out int tx, out int ty))
            {
                if (grid.IsWalkable(tx, ty))
                    return grid.CellToWorld(tx, ty);
                if (grid.FindNearestWalkable(tx, ty, out int wx, out int wy))
                    return grid.CellToWorld(wx, wy);
            }
            return quadCenter;
        }
        var positions = GetInteractionTileWorldPositions();
        if (positions != null && positions.Count > 0)
            return positions[0];
        return transform.position;
    }

    /// <summary>True if the given world position is within the middle of one of the highlight quads.</summary>
    public bool IsEmployeeOnInteractionTile(Vector3 worldPosition)
    {
        var fromModel = GetInteractionPositionsFromModel();
        if (fromModel != null && fromModel.Count > 0)
        {
            float r = interactionRadius > 0f ? interactionRadius : (grid != null ? grid.cellSize * 0.65f : 0.65f);
            float rSq = r * r;
            float ex = worldPosition.x;
            float ez = worldPosition.z;
            if (!grid.WorldToCell(worldPosition, out int empCellX, out int empCellY))
                empCellX = empCellY = -999;
            for (int i = 0; i < fromModel.Count; i++)
            {
                Vector3 c = fromModel[i];
                float dx = ex - c.x;
                float dz = ez - c.z;
                if (dx * dx + dz * dz <= rSq)
                    return true;
                if (grid != null && grid.WorldToCell(c, out int quadCellX, out int quadCellY) && empCellX == quadCellX && empCellY == quadCellY)
                    return true;
            }
            return false;
        }
        if (grid == null || interactionTileOffsets == null) return false;
        if (!grid.WorldToCell(worldPosition, out int exi, out int ey)) return false;
        GetOriginCell(out int ox, out int oy);
        foreach (var offset in interactionTileOffsets)
        {
            if (ox + offset.x == exi && oy + offset.y == ey)
                return true;
        }
        return false;
    }
}
