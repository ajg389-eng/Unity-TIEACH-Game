using UnityEngine;

public class GridManager : MonoBehaviour
{
    public Transform floor;
    public float cellSize = 1f;
    public bool drawGizmos = true;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Node[,] Nodes { get; private set; }
    public Vector3 Origin { get; private set; }

    void Awake() => RebuildFromFloor();

    [ContextMenu("Rebuild Grid From Floor")]

    public void SetGridVisible(bool visible)
    {
        drawGizmos = visible;
    }
    public void RebuildFromFloor()
    {
        if (!floor) { Debug.LogError("Assign floor Transform."); return; }

        var r = floor.GetComponentInChildren<Renderer>();
        Bounds b;
        if (r != null) b = r.bounds;
        else
        {
            Vector3 size = new Vector3(floor.localScale.x * 10f, 0f, floor.localScale.z * 10f);
            b = new Bounds(floor.position, size);
        }

        Width = Mathf.Max(1, Mathf.FloorToInt(b.size.x / cellSize));
        Height = Mathf.Max(1, Mathf.FloorToInt(b.size.z / cellSize));

        Origin = new Vector3(b.min.x, floor.position.y, b.min.z);

        Nodes = new Node[Width, Height];
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                Vector3 world = CellToWorld(x, y);
                Nodes[x, y] = new Node(x, y, world);
            }
    }

    public Vector3 CellToWorld(int x, int y)
        => Origin + new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);

    public bool WorldToCell(Vector3 world, out int x, out int y)
    {
        x = Mathf.FloorToInt((world.x - Origin.x) / cellSize);
        y = Mathf.FloorToInt((world.z - Origin.z) / cellSize);
        return x >= 0 && y >= 0 && x < Width && y < Height;
    }

    public bool CanPlace(int startX, int startY, int sizeX, int sizeY)
    {
        for (int x = startX; x < startX + sizeX; x++)
            for (int y = startY; y < startY + sizeY; y++)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
                if (Nodes[x, y].occupied) return false;
            }
        return true;
    }

    public void SetOccupied(int startX, int startY, int sizeX, int sizeY, bool occ)
    {
        for (int x = startX; x < startX + sizeX; x++)
            for (int y = startY; y < startY + sizeY; y++)
                Nodes[x, y].occupied = occ;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || Nodes == null) return;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var n = Nodes[x, y];
                Gizmos.color = n.walkable ? new Color(0.7f, 0.7f, 0.7f, 0.35f) : new Color(1f, 0.2f, 0.2f, 0.5f);
                Gizmos.DrawCube(n.world + Vector3.up * 0.02f, new Vector3(cellSize * 0.95f, 0.02f, cellSize * 0.95f));
            }
    }
}

public class Node
{
    public int x, y;
    public Vector3 world;
    public bool walkable = true;
    public bool occupied = false;

    public Node(int x, int y, Vector3 world)
    {
        this.x = x;
        this.y = y;
        this.world = world;
    }
}