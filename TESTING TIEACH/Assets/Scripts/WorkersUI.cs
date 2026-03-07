using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Management screen Workers tab: shows current worker count, hire cost, and a Hire button.
/// Assign references in the Inspector or they are found in children.
/// </summary>
public class WorkersUI : MonoBehaviour
{
    [Tooltip("Shows current number of employees")]
    public TextMeshProUGUI countText;
    [Tooltip("Shows hire cost (e.g. Cost: $100)")]
    public TextMeshProUGUI costText;
    [Tooltip("Click to hire one worker")]
    public Button hireButton;

    ProductionManager production;
    bool listenerAdded;

    void Start()
    {
        EnsureRefs();
    }

    void OnEnable()
    {
        EnsureRefs();
        Refresh();
    }

    void EnsureRefs()
    {
        if (production == null)
            production = ProductionManager.Instance != null ? ProductionManager.Instance : FindObjectOfType<ProductionManager>();
        if (countText == null) countText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (hireButton == null) hireButton = GetComponentInChildren<Button>(true);
        if (hireButton != null && !listenerAdded)
        {
            hireButton.onClick.AddListener(OnHireClicked);
            listenerAdded = true;
        }
    }

    void OnHireClicked()
    {
        if (production == null)
        {
            production = FindObjectOfType<ProductionManager>();
            if (production == null) return;
        }
        if (production.HireWorker())
            Refresh();
        else
            Refresh(); // update status so user sees why it failed
    }

    void Refresh()
    {
        EnsureRefs();

        if (production == null)
        {
            if (countText != null) countText.text = "Workers: —";
            if (costText != null) costText.text = "Cost: — (No ProductionManager in scene)";
            if (hireButton != null) hireButton.interactable = false;
            return;
        }

        int count = production.employees != null ? production.employees.Count : 0;
        if (countText != null)
            countText.text = "Workers: " + count;

        bool canHire = production.employeePrefab != null;
        var money = FindObjectOfType<MoneyManager>();
        string status = "";
        if (production.employeePrefab == null)
        {
            canHire = false;
            status = " — Assign Employee Prefab on ProductionManager.";
        }
        else if (money != null && !money.CanAfford(production.hireCost))
        {
            canHire = false;
            status = " — Not enough money.";
        }

        if (costText != null)
            costText.text = "Cost: $" + production.hireCost + status;

        if (hireButton != null)
            hireButton.interactable = canHire;
    }
}
