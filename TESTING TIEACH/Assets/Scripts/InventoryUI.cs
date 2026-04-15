using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public GameModeManager modeManager;
    public InventoryManager inventory;

    [Header("Build Mode Button")]
    public Button inventoryButton;        // the "Inventory" button (only build mode)
    public GameObject panel;              // popup panel (menu)

    [Header("List")]
    public Transform contentParent;       // Vertical Layout Group content
    public GameObject rowPrefab;          // prefab for one row

    void Start()
    {
        if (inventoryButton)
            inventoryButton.onClick.AddListener(TogglePanel);

        RefreshAll();
        ApplyModeState();
    }

    void Update()
    {
        // Inventory controls Build mode now: open panel => Build, closed => Play.
        ApplyModeState();
    }

    void ApplyModeState()
    {
        if (inventoryButton) inventoryButton.gameObject.SetActive(true);
        if (modeManager == null || panel == null) return;
        modeManager.SetMode(panel.activeSelf ? GameModeManager.Mode.Build : GameModeManager.Mode.Play);
    }

    public void TogglePanel()
    {
        if (!panel) return;
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) RefreshAll();
    }

    public void RefreshAll()
    {
        if (!inventory || !contentParent || !rowPrefab) return;

        // clear old rows
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        foreach (var item in inventory.allItems)
        {
            if (item == null) continue;

            var row = Instantiate(rowPrefab, contentParent);

            // Row references (updated for your hierarchy)
            var nameButton = row.transform.Find("NameButton").GetComponent<Button>();

            // Get TMP text inside NameButton (works even if it's named "Text (TMP)")
            var nameText = row.transform.Find("NameButton").GetComponentInChildren<TextMeshProUGUI>(true);

            var qtyText = row.transform.Find("QtyBack/QtyText").GetComponent<TextMeshProUGUI>();

            var buyButton = row.transform.Find("BuyButton").GetComponent<Button>();

            var priceText = row.transform.Find("PriceBack/Price").GetComponent<TextMeshProUGUI>();

            nameText.text = item.itemName;
            qtyText.text = inventory.GetCount(item).ToString();
            priceText.text = "$" + item.price;

            nameButton.onClick.AddListener(() =>
            {
                inventory.SelectItem(item);


                // Tell placer to begin placement
                var placer = FindObjectOfType<BuildPlacer>();
                if (placer) placer.BeginPlacement(item);
            });

            buyButton.onClick.AddListener(() =>
            {
                bool bought = inventory.PurchaseOne(item);
                if (bought) qtyText.text = inventory.GetCount(item).ToString();
            });
        }
    }
}