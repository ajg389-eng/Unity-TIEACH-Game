using System.Collections.Generic;
using UnityEngine;

public struct ProductionJob
{
    public CustomerOrder order;
    public Register register;
}

/// <summary>
/// Creates production jobs from register orders and assigns them to employees.
/// Employees walk: freezer -> grill -> pantry -> assembly -> register.
/// Place one of each station (Freezer, Grill, Pantry, Assembly) in the scene; assign employees.
/// </summary>
public class ProductionManager : MonoBehaviour
{
    public static ProductionManager Instance { get; private set; }

    [Header("Order config (to know patty vs ingredients)")]
    public CustomerOrderConfig orderConfig;

    [Header("Registers")]
    public List<Register> registers = new List<Register>();

    [Header("Employees")]
    public List<KitchenEmployee> employees = new List<KitchenEmployee>();

    [Header("Hiring")]
    [Tooltip("Prefab with KitchenEmployee to spawn when hiring (create via Production > Create Default Employee Prefab if needed)")]
    public GameObject employeePrefab;
    [Tooltip("Where to spawn new employees; if unset uses this transform")]
    public Transform spawnPoint;
    public int hireCost = 100;

    readonly List<ProductionJob> pendingJobs = new List<ProductionJob>();
    MoneyManager moneyManager;

    FreezerStation freezer;
    GrillStation grill;
    PantryStation pantry;
    AssemblyStation assembly;

    public ItemDefinition PattyItem => orderConfig != null ? orderConfig.burgerBase : null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        moneyManager = FindObjectOfType<MoneyManager>();
    }

    /// <summary>Hire one worker: spend money (if MoneyManager present) and spawn employee from prefab. Returns true if hired.</summary>
    public bool HireWorker()
    {
        if (employeePrefab == null)
        {
            Debug.LogWarning("ProductionManager: no employeePrefab assigned. Use Production > Create Default Employee Prefab and assign it.");
            return false;
        }
        if (moneyManager != null && !moneyManager.CanAfford(hireCost))
            return false;
        if (moneyManager != null)
            moneyManager.TrySpend(hireCost);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject go = Instantiate(employeePrefab, pos, Quaternion.identity);
        var emp = go.GetComponent<KitchenEmployee>();
        if (emp != null)
            RegisterEmployee(emp);
        return true;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        RefreshStations();
        CollectJobsFromRegisters();
        AssignJobsToEmployees();
    }

    void RefreshStations()
    {
        if (freezer == null) freezer = FindObjectOfType<FreezerStation>();
        if (grill == null) grill = FindObjectOfType<GrillStation>();
        if (pantry == null) pantry = FindObjectOfType<PantryStation>();
        if (assembly == null) assembly = FindObjectOfType<AssemblyStation>();
    }

    void CollectJobsFromRegisters()
    {
        foreach (var reg in registers)
        {
            if (reg == null || !reg.isEnabled) continue;
            if (reg.HasPreparedOrder) continue;

            CustomerOrder order = reg.GetFrontCustomerOrder();
            if (order == null || order.lines == null || order.lines.Count == 0) continue;

            bool alreadyPending = false;
            foreach (var j in pendingJobs)
                if (j.register == reg) { alreadyPending = true; break; }
            if (alreadyPending) continue;

            pendingJobs.Add(new ProductionJob { order = order.Clone(), register = reg });
        }
    }

    void AssignJobsToEmployees()
    {
        for (int i = pendingJobs.Count - 1; i >= 0; i--)
        {
            var job = pendingJobs[i];
            KitchenEmployee emp = null;
            foreach (var e in employees)
            {
                if (e != null && e.IsIdle)
                {
                    emp = e;
                    break;
                }
            }
            if (emp == null) break;
            emp.AssignJob(job);
            pendingJobs.RemoveAt(i);
        }
    }

    public void RegisterEmployee(KitchenEmployee emp)
    {
        if (emp != null && !employees.Contains(emp))
            employees.Add(emp);
    }

    public void UnregisterEmployee(KitchenEmployee emp)
    {
        employees.Remove(emp);
    }

    public Vector3 GetFreezerPosition()
    {
        return freezer != null ? freezer.GetInteractionPosition() : transform.position;
    }

    public Vector3 GetGrillPosition()
    {
        return grill != null ? grill.GetInteractionPosition() : transform.position;
    }

    public Vector3 GetPantryPosition(ItemDefinition item)
    {
        return pantry != null ? pantry.GetInteractionPosition() : transform.position;
    }

    public Vector3 GetAssemblyPosition()
    {
        return assembly != null ? assembly.GetInteractionPosition() : transform.position;
    }

    public bool PlacePattyOnGrill()
    {
        if (grill == null || !grill.CanPlacePatty()) return false;
        grill.PlacePatty();
        return true;
    }

    public bool IsGrillCooked()
    {
        return grill != null && grill.IsCooked();
    }

    public bool TakePattyFromGrill()
    {
        return grill != null && grill.TakeCookedPatty();
    }

    public bool TakeFromPantry(ItemDefinition item)
    {
        return pantry != null && pantry.TakeItem(item);
    }
}
