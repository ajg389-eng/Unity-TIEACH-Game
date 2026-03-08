using System.Collections.Generic;
using UnityEngine;

/// <summary>Station types in production order. Workers are assigned 1-4 of these by the player.</summary>
public enum StationType
{
    Freezer,
    Grill,
    Pantry,
    Assembly
}

public class ProductionJob
{
    public CustomerOrder order;
    public Register register;
    /// <summary>0=freezer, 1=grill, 2=pantry, 3=assembly, 4=deliver.</summary>
    public int currentStepIndex;
    public KitchenEmployee assignedTo;

    public ProductionJob(CustomerOrder o, Register r)
    {
        order = o;
        register = r;
        currentStepIndex = 0;
        assignedTo = null;
    }
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

    /// <summary>Hire one worker: spend money (if MoneyManager present) and spawn employee from prefab. Returns the new employee or null.</summary>
    public KitchenEmployee HireWorker()
    {
        if (employeePrefab == null)
        {
            Debug.LogWarning("ProductionManager: no employeePrefab assigned. Use Production > Create Default Employee Prefab and assign it.");
            return null;
        }
        if (moneyManager != null && !moneyManager.CanAfford(hireCost))
            return null;
        if (moneyManager != null)
            moneyManager.TrySpend(hireCost);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject go = Instantiate(employeePrefab, pos, Quaternion.identity);
        var emp = go.GetComponent<KitchenEmployee>();
        if (emp != null)
            RegisterEmployee(emp);
        return emp;
    }

    /// <summary>Fire a worker: unregister and destroy. Call from management UI.</summary>
    public void FireWorker(KitchenEmployee emp)
    {
        if (emp == null) return;
        UnregisterEmployee(emp);
        Destroy(emp.gameObject);
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

            pendingJobs.Add(new ProductionJob(order.Clone(), reg));
        }
    }

    void AssignJobsToEmployees()
    {
        foreach (var e in employees)
        {
            if (e == null || !e.IsIdle || !e.HasAllStationsAssigned()) continue;
            foreach (var job in pendingJobs)
            {
                if (job.assignedTo != null || job.currentStepIndex >= 4) continue;
                if (!e.HasStationForStep(job.currentStepIndex)) continue;
                job.assignedTo = e;
                e.AssignJob(job);
                break;
            }
        }
    }

    /// <summary>Advances the job to the next step. Call when employee finishes a step.</summary>
    public void AdvanceJobStep(ProductionJob job)
    {
        if (job != null && job.currentStepIndex < 4)
            job.currentStepIndex++;
    }

    /// <summary>Releases the job so another worker can take the next step. Call when employee finishes step 0, 1, or 2.</summary>
    public void ReleaseJob(ProductionJob job)
    {
        if (job != null)
            job.assignedTo = null;
    }

    /// <summary>Completes and removes the job (after delivery). Call when employee delivers order.</summary>
    public void CompleteJob(ProductionJob job)
    {
        if (job == null) return;
        job.assignedTo = null;
        pendingJobs.Remove(job);
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

    public Vector3 GetFreezerPosition(KitchenEmployee forEmployee = null)
    {
        if (forEmployee != null)
        {
            var fs = forEmployee.GetFreezerStation();
            if (fs != null) return fs.GetInteractionPosition();
        }
        return freezer != null ? freezer.GetInteractionPosition() : transform.position;
    }

    /// <summary>Grill this employee uses (their target grill if set, else their work-at grill, else default).</summary>
    public GrillStation GetGrillFor(KitchenEmployee emp)
    {
        if (emp != null && emp.targetGrill != null) return emp.targetGrill;
        if (emp != null)
        {
            var gs = emp.GetGrillStation();
            if (gs != null) return gs;
        }
        return grill;
    }

    public Vector3 GetGrillPosition(KitchenEmployee forEmployee = null)
    {
        var g = GetGrillFor(forEmployee);
        return g != null ? g.GetInteractionPosition() : transform.position;
    }

    public Vector3 GetPantryPosition(ItemDefinition item, KitchenEmployee forEmployee = null)
    {
        if (forEmployee != null)
        {
            var ps = forEmployee.GetPantryStation();
            if (ps != null) return ps.GetInteractionPosition();
        }
        return pantry != null ? pantry.GetInteractionPosition() : transform.position;
    }

    public Vector3 GetAssemblyPosition(KitchenEmployee forEmployee = null)
    {
        if (forEmployee != null)
        {
            var ast = forEmployee.GetAssemblyStation();
            if (ast != null) return ast.GetInteractionPosition();
        }
        return assembly != null ? assembly.GetInteractionPosition() : transform.position;
    }

    /// <summary>True if the position is on one of this station's interaction tiles (worker must stand here to use it).</summary>
    public bool IsEmployeeOnFreezerTile(Vector3 worldPosition, KitchenEmployee forEmployee = null)
    {
        var f = forEmployee != null ? forEmployee.GetFreezerStation() : null;
        if (f == null) f = freezer;
        var tiles = f != null ? f.GetComponent<StationInteractionTiles>() : null;
        return tiles == null || tiles.IsEmployeeOnInteractionTile(worldPosition);
    }
    public bool IsEmployeeOnGrillTile(Vector3 worldPosition, KitchenEmployee forEmployee = null)
    {
        var g = GetGrillFor(forEmployee);
        var tiles = g != null ? g.GetComponent<StationInteractionTiles>() : null;
        return tiles == null || tiles.IsEmployeeOnInteractionTile(worldPosition);
    }
    public bool IsEmployeeOnPantryTile(Vector3 worldPosition, KitchenEmployee forEmployee = null)
    {
        var p = forEmployee != null ? forEmployee.GetPantryStation() : null;
        if (p == null) p = pantry;
        var tiles = p != null ? p.GetComponent<StationInteractionTiles>() : null;
        return tiles == null || tiles.IsEmployeeOnInteractionTile(worldPosition);
    }
    public bool IsEmployeeOnAssemblyTile(Vector3 worldPosition, KitchenEmployee forEmployee = null)
    {
        var a = forEmployee != null ? forEmployee.GetAssemblyStation() : null;
        if (a == null) a = assembly;
        var tiles = a != null ? a.GetComponent<StationInteractionTiles>() : null;
        return tiles == null || tiles.IsEmployeeOnInteractionTile(worldPosition);
    }

    public bool PlacePattyOnGrill(KitchenEmployee forEmployee = null)
    {
        var g = GetGrillFor(forEmployee);
        if (g == null || !g.CanPlacePatty()) return false;
        g.PlacePatty();
        return true;
    }

    public bool IsGrillCooked(KitchenEmployee forEmployee = null)
    {
        var g = GetGrillFor(forEmployee);
        return g != null && g.IsCooked();
    }

    public bool TakePattyFromGrill(KitchenEmployee forEmployee = null)
    {
        var g = GetGrillFor(forEmployee);
        return g != null && g.TakeCookedPatty();
    }

    public bool TakeFromPantry(ItemDefinition item, KitchenEmployee forEmployee = null)
    {
        var p = forEmployee != null ? forEmployee.GetPantryStation() : null;
        if (p == null) p = pantry;
        return p != null && p.TakeItem(item);
    }

    public float GetFreezerInteractionTime(KitchenEmployee forEmployee = null)
    {
        var f = forEmployee != null ? forEmployee.GetFreezerStation() : null;
        if (f == null) f = freezer;
        return f != null ? f.interactionTimeSeconds : 1f;
    }
    public float GetGrillPlaceTime(KitchenEmployee forEmployee = null) => GetGrillFor(forEmployee) != null ? GetGrillFor(forEmployee).placeTimeSeconds : 0.5f;
    public float GetGrillCookTime(KitchenEmployee forEmployee = null) => GetGrillFor(forEmployee) != null ? GetGrillFor(forEmployee).cookTimeSeconds : 4f;
    public float GetGrillWaitAfterCookedTime(KitchenEmployee forEmployee = null) => GetGrillFor(forEmployee) != null ? GetGrillFor(forEmployee).waitAfterCookedSeconds : 0.5f;
    public float GetGrillTakeTime(KitchenEmployee forEmployee = null) => GetGrillFor(forEmployee) != null ? GetGrillFor(forEmployee).takeTimeSeconds : 0.5f;
    public float GetPantryInteractionTime(KitchenEmployee forEmployee = null)
    {
        var p = forEmployee != null ? forEmployee.GetPantryStation() : null;
        if (p == null) p = pantry;
        return p != null ? p.interactionTimeSeconds : 0.5f;
    }
    public float GetAssemblyInteractionTime(KitchenEmployee forEmployee = null)
    {
        var a = forEmployee != null ? forEmployee.GetAssemblyStation() : null;
        if (a == null) a = assembly;
        return a != null ? a.interactionTimeSeconds : 1f;
    }
}
