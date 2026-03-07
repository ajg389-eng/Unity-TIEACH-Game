using UnityEngine;
using TMPro;

public class BuildPlacer : MonoBehaviour
{
    public GameModeManager modeManager;
    public GridManager grid;
    public InventoryManager inventory;

    [Header("Raycast")]
    public LayerMask floorLayer;

    [Header("UI")]
    public TextMeshProUGUI placementHintText; // "LMB place | ESC cancel"

    private ItemDefinition placingItem;
    private GameObject ghost;

    public bool IsPlacing => placingItem != null;

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
            return;
        }

        if (!IsPlacing) return;

        // Cancel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        // Move ghost to hovered cell
        if (TryGetHoveredCell(out int x, out int y))
        {
            if (ghost)
                ghost.transform.position = grid.CellToWorld(x, y);

            // Place on left click
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

        // Optional: disable colliders so raycasts don’t hit the ghost
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

    void SetHint(bool show)
    {
        if (!placementHintText) return;
        placementHintText.gameObject.SetActive(show);
        if (show) placementHintText.text = "LMB: Place    ESC: Cancel";
    }

    bool TryGetHoveredCell(out int x, out int y)
    {
        x = y = 0;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, floorLayer))
            return false;

        return grid.WorldToCell(hit.point, out x, out y);
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