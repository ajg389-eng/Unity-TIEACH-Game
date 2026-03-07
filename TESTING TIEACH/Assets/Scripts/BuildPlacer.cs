using UnityEngine;
using TMPro;

public class BuildPlacer : MonoBehaviour
{
    public GameModeManager modeManager;
    public GridManager grid;
    public InventoryManager inventory;

    [Header("Raycast")]
    public LayerMask floorLayer;
    [Tooltip("Layers to raycast for drag-to-move. Leave as Nothing to use Everything. Or set to a 'Placeable' layer and put station prefabs on that layer so the floor doesn't block clicks.")]
    public LayerMask placeableLayer;

    [Header("UI")]
    public TextMeshProUGUI placementHintText; // "LMB place | ESC cancel"

    private ItemDefinition placingItem;
    private GameObject ghost;

    private GameObject draggingObject;
    private BuildFootprint dragFootprint;
    private int dragOrigX, dragOrigY;

    public bool IsPlacing => placingItem != null;
    public bool IsDragging => draggingObject != null;

    void Start()
    {
        SetHint(false);
    }

    void Update()
    {
        if (!modeManager || !grid || !inventory) return;

        // Only allow placement in Build mode
        if (modeManager.CurrentMode != GameModeManager.Mode.Build)
        {
            CancelPlacement();
            CancelDrag();
            return;
        }

        // Cancel (placement or drag)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsDragging)
                CancelDrag();
            else
                CancelPlacement();
            return;
        }

        // ---- Dragging a placed object ----
        if (IsDragging)
        {
            if (Input.GetMouseButtonDown(1))
            {
                RemoveDraggedAndReturnToInventory();
                return;
            }
            if (TryGetHoveredCell(out int dx, out int dy))
            {
                draggingObject.transform.position = grid.CellToWorld(dx, dy);
                if (Input.GetMouseButtonDown(0))
                    TryPlaceDraggedAt(dx, dy);
            }
            return;
        }

        // ---- Placing new item from inventory ----
        if (!IsPlacing)
        {
            if (Input.GetMouseButtonDown(0))
                TryStartDrag();
            return;
        }

        // Move ghost to hovered cell
        if (TryGetHoveredCell(out int x, out int y))
        {
            if (ghost)
                ghost.transform.position = grid.CellToWorld(x, y);

            if (Input.GetMouseButtonDown(0))
                TryPlaceAt(x, y);
        }
    }

    public void BeginPlacement(ItemDefinition item)
    {
        if (item == null || item.prefab == null) return;

        placingItem = item;

        // Make a ghost preview
        if (ghost) Destroy(ghost);
        ghost = Instantiate(item.prefab);

        MakeTranslucent(ghost, 0.7f); // 0.5 = 50% transparent

        // Optional: disable colliders so raycasts donùt hit the ghost
        foreach (var c in ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        SetHint(true);
    }

    public void CancelPlacement()
    {
        placingItem = null;

        if (ghost) Destroy(ghost);
        ghost = null;

        SetHint(false);
    }

    void MakeTranslucent(GameObject obj, float alpha)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                // Switch material to transparent mode (URP compatible)
                mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;

                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    void SetHint(bool show, bool dragging = false)
    {
        if (!placementHintText) return;
        placementHintText.gameObject.SetActive(show);
        if (show) placementHintText.text = dragging ? "LMB: Place    RMB: Remove & return to inventory    ESC: Cancel" : "LMB: Place    ESC: Cancel";
    }

    bool TryGetHoveredCell(out int x, out int y)
    {
        x = y = 0;
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, floorLayer))
            return false;

        return grid.WorldToCell(hit.point, out x, out y);
    }

    void TryStartDrag()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = placeableLayer.value != 0 ? placeableLayer.value : -1;
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, layerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            var fp = hit.collider.GetComponentInParent<BuildFootprint>();
            if (fp == null) continue;

            GameObject root = fp.gameObject;
            if (root == ghost) continue;

            if (!grid.WorldToCell(root.transform.position, out int ox, out int oy))
                continue;

            int sizeX = Mathf.Max(1, fp.sizeX);
            int sizeY = Mathf.Max(1, fp.sizeY);

            grid.SetOccupied(ox, oy, sizeX, sizeY, false);
            draggingObject = root;
            dragFootprint = fp;
            dragOrigX = ox;
            dragOrigY = oy;
            SetHint(true, true);
            return;
        }
    }

    void TryPlaceDraggedAt(int x, int y)
    {
        if (draggingObject == null || dragFootprint == null) return;

        int sizeX = Mathf.Max(1, dragFootprint.sizeX);
        int sizeY = Mathf.Max(1, dragFootprint.sizeY);

        if (!grid.CanPlace(x, y, sizeX, sizeY))
            return;

        draggingObject.transform.position = grid.CellToWorld(x, y);
        grid.SetOccupied(x, y, sizeX, sizeY, true);
        EndDrag();
    }

    void CancelDrag()
    {
        if (draggingObject == null || dragFootprint == null) return;

        draggingObject.transform.position = grid.CellToWorld(dragOrigX, dragOrigY);
        grid.SetOccupied(dragOrigX, dragOrigY, Mathf.Max(1, dragFootprint.sizeX), Mathf.Max(1, dragFootprint.sizeY), true);
        EndDrag();
    }

    void EndDrag()
    {
        draggingObject = null;
        dragFootprint = null;
        SetHint(IsPlacing);
    }

    void RemoveDraggedAndReturnToInventory()
    {
        if (draggingObject == null) return;

        var pbi = draggingObject.GetComponent<PlacedBuildItem>();
        if (pbi != null && pbi.itemDefinition != null && inventory != null)
            inventory.AddOne(pbi.itemDefinition);

        Object.Destroy(draggingObject);
        draggingObject = null;
        dragFootprint = null;
        SetHint(IsPlacing);

        var invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null) invUI.RefreshAll();
    }

    void TryPlaceAt(int x, int y)
    {
        if (placingItem == null) return;

        // Must own one to place
        if (inventory.GetCount(placingItem) <= 0) return;

        int sizeX = 1, sizeY = 1;
        var fp = placingItem.prefab.GetComponent<BuildFootprint>();
        if (fp != null) { sizeX = fp.sizeX; sizeY = fp.sizeY; }

        // Check space
        if (!grid.CanPlace(x, y, sizeX, sizeY)) return;

        // Consume inventory
        if (!inventory.TryConsumeOne(placingItem)) return;

        // Place real object
        var placed = Instantiate(placingItem.prefab);
        placed.transform.position = grid.CellToWorld(x, y);
        var pbi = placed.AddComponent<PlacedBuildItem>();
        pbi.itemDefinition = placingItem;

        // Refresh inventory UI so quantities update
        var invUI = FindObjectOfType<InventoryUI>();
        if (invUI) invUI.RefreshAll();

        // Mark occupied + not walkable
        grid.SetOccupied(x, y, sizeX, sizeY, true);

        // Keep placing until user cancels (or you can auto-cancel if you want)
        // If you want auto-cancel after 1 placement, uncomment:
        // CancelPlacement();
    }
}