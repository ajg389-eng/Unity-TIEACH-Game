using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One row in the assignment UI: work at one station, deliver product to another.</summary>
[Serializable]
public class AssignmentRow
{
    [Tooltip("Station where the employee does the action (e.g. Freezer, Grill).")]
    public GameObject workAt;
    [Tooltip("Station the employee brings the product to after the action (e.g. Grill, Register).")]
    public GameObject deliverTo;
}

/// <summary>
/// Hired employee. Does nothing until the player assigns 4 rows (work at + deliver to). Only workers with all 4 rows filled get orders.
/// </summary>
public class KitchenEmployee : MonoBehaviour
{
    public float moveSpeed = 3f;
    [Tooltip("Time to ease into direction changes (higher = smoother, less snappy).")]
    public float moveSmoothTime = 0.12f;
    [Tooltip("Y position to keep the employee on the ground. If 0 and grid is set, uses grid floor height.")]
    public float groundHeight;
    [Tooltip("Assign GridManager so employee pathfinds on the grid. If null, falls back to straight-line movement.")]
    public GridManager grid;

    [Header("Identity")]
    [Tooltip("Display name shown in the management screen. Editable by the player.")]
    public string employeeName = "Worker";

    [Header("Assignment (4 rows: work at station → deliver to station). Synced to legacy lists for workflow.)")]
    [Tooltip("Row 0 = Freezer→Grill, Row 1 = Grill→…, Row 2 = Pantry→…, Row 3 = Assembly→Register. Fill via in-world assignment panel.")]
    public AssignmentRow[] assignmentRows = new AssignmentRow[4];

    [Header("Legacy (synced from assignmentRows; used by workflow)")]
    [Tooltip("Stations this worker is assigned to. Populated from assignmentRows.")]
    public List<StationType> assignedStations = new List<StationType>();
    [Tooltip("Grill to deliver patty to (from row 0 deliverTo). Set when assigning in panel.")]
    public GrillStation targetGrill;

    ProductionJob currentJob;

    public bool IsIdle => currentJob == null;
    public bool HasJob => currentJob != null;
    public int AssignedStationCount => assignedStations != null ? assignedStations.Count : 0;

    public bool HasStationForStep(int stepIndex)
    {
        if (assignedStations == null || stepIndex < 0 || stepIndex > 3) return false;
        StationType s = (StationType)stepIndex;
        return assignedStations.Contains(s);
    }

    public bool HasAllStationsAssigned()
    {
        EnsureAssignmentRows();
        if (assignmentRows == null || assignmentRows.Length < 4) return false;
        for (int i = 0; i < 4; i++)
        {
            if (assignmentRows[i] == null || assignmentRows[i].workAt == null) return false;
            if (i == 0 && assignmentRows[0].deliverTo == null) return false;
        }
        SyncFromAssignmentRows();
        if (assignedStations == null || assignedStations.Count != 4) return false;
        for (int i = 0; i < 4; i++)
            if (!assignedStations.Contains((StationType)i)) return false;
        return true;
    }

    /// <summary>Set which stations this worker is assigned to (1–4). Only workers with all 4 receive orders.</summary>
    public void SetAssignedStations(List<StationType> stations)
    {
        assignedStations = new List<StationType>();
        if (stations == null) return;
        for (int i = 0; i < stations.Count && assignedStations.Count < 4; i++)
        {
            if (!assignedStations.Contains(stations[i]))
                assignedStations.Add(stations[i]);
        }
    }

    public void EnsureAssignmentRows()
    {
        if (assignmentRows == null) assignmentRows = new AssignmentRow[4];
        for (int i = 0; i < 4; i++)
        {
            if (assignmentRows[i] == null)
                assignmentRows[i] = new AssignmentRow();
        }
    }

    /// <summary>Sync assignedStations and targetGrill from the 4 assignment rows so the workflow can run.</summary>
    public void SyncFromAssignmentRows()
    {
        EnsureAssignmentRows();
        assignedStations = new List<StationType>();
        targetGrill = null;
        for (int i = 0; i < 4 && i < assignmentRows.Length; i++)
        {
            var row = assignmentRows[i];
            if (row == null) continue;
            StationType? t = GetStationTypeFrom(row.workAt);
            if (t.HasValue && !assignedStations.Contains(t.Value))
                assignedStations.Add(t.Value);
            if (i == 0 && row.workAt != null && row.deliverTo != null)
            {
                var fs = row.workAt.GetComponent<FreezerStation>();
                var gs = row.deliverTo.GetComponent<GrillStation>();
                if (fs != null && gs != null)
                    targetGrill = gs;
            }
        }
    }

    /// <summary>Return station type for workflow (Freezer, Grill, Pantry, Assembly). Returns null if not a known station.</summary>
    public static StationType? GetStationTypeFrom(GameObject go)
    {
        if (go == null) return null;
        if (go.GetComponent<FreezerStation>() != null) return StationType.Freezer;
        if (go.GetComponent<GrillStation>() != null) return StationType.Grill;
        if (go.GetComponent<PantryStation>() != null) return StationType.Pantry;
        if (go.GetComponent<AssemblyStation>() != null) return StationType.Assembly;
        return null;
    }

    public static bool IsRegister(GameObject go) => go != null && go.GetComponent<Register>() != null;

    /// <summary>Get interaction position for any station or register.</summary>
    public static Vector3 GetInteractionPosition(GameObject go)
    {
        if (go == null) return Vector3.zero;
        var f = go.GetComponent<FreezerStation>();
        if (f != null) return f.GetInteractionPosition();
        var g = go.GetComponent<GrillStation>();
        if (g != null) return g.GetInteractionPosition();
        var p = go.GetComponent<PantryStation>();
        if (p != null) return p.GetInteractionPosition();
        var a = go.GetComponent<AssemblyStation>();
        if (a != null) return a.GetInteractionPosition();
        var r = go.GetComponent<Register>();
        if (r != null) return r.transform.position;
        return go.transform.position;
    }

    public FreezerStation GetFreezerStation()
    {
        EnsureAssignmentRows();
        for (int i = 0; i < 4 && i < assignmentRows.Length; i++)
        {
            if (assignmentRows[i]?.workAt == null) continue;
            var fs = assignmentRows[i].workAt.GetComponent<FreezerStation>();
            if (fs != null) return fs;
        }
        return null;
    }

    public GrillStation GetGrillStation()
    {
        EnsureAssignmentRows();
        for (int i = 0; i < 4 && i < assignmentRows.Length; i++)
        {
            if (assignmentRows[i]?.workAt == null) continue;
            var gs = assignmentRows[i].workAt.GetComponent<GrillStation>();
            if (gs != null) return gs;
        }
        return null;
    }

    public PantryStation GetPantryStation()
    {
        EnsureAssignmentRows();
        for (int i = 0; i < 4 && i < assignmentRows.Length; i++)
        {
            if (assignmentRows[i]?.workAt == null) continue;
            var ps = assignmentRows[i].workAt.GetComponent<PantryStation>();
            if (ps != null) return ps;
        }
        return null;
    }

    public AssemblyStation GetAssemblyStation()
    {
        EnsureAssignmentRows();
        for (int i = 0; i < 4 && i < assignmentRows.Length; i++)
        {
            if (assignmentRows[i]?.workAt == null) continue;
            var ast = assignmentRows[i].workAt.GetComponent<AssemblyStation>();
            if (ast != null) return ast;
        }
        return null;
    }

    float stateTimer;

    List<Vector3> path = new List<Vector3>();
    Vector3 pathDestination;
    Vector3 moveVelocity;
    const float ArrivalRadius = 0.25f;

    enum Step
    {
        None,
        GoToFreezer, AtFreezer,
        GoToGrill, AtGrill,
        GoToPantry, AtPantry,
        GoToAssembly, AtAssembly,
        GoToRegister, AtRegister
    }

    Step step;
    bool hasPatty;
    readonly List<ItemDefinition> ingredientsHeld = new List<ItemDefinition>();

    ProductionManager manager;

    float instantStepBarTimer;

    public bool ShowTaskBar { get; private set; }
    public float TaskProgress { get; private set; }

    void Start()
    {
        EnsureAssignmentRows();
        SyncFromAssignmentRows();
        manager = ProductionManager.Instance;
        if (manager != null)
            manager.RegisterEmployee(this);
        if (grid == null)
            grid = FindObjectOfType<GridManager>();
        if (groundHeight == 0f && grid != null)
            groundHeight = grid.Origin.y;
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.UnregisterEmployee(this);
    }

    public void AssignJob(ProductionJob job)
    {
        if (currentJob != null) return;
        currentJob = job;
        step = Step.GoToFreezer;
        stateTimer = 0f;
        hasPatty = false;
        ingredientsHeld.Clear();
    }

    void Update()
    {
        SnapToGround();
        if (currentJob == null || manager == null) return;
        RunWorkflow();
    }

    void RunWorkflow()
    {
        ShowTaskBar = false;
        TaskProgress = 0f;

        switch (step)
        {
            case Step.GoToFreezer:
                if (MoveToward(manager.GetFreezerPosition(this)))
                {
                    if (manager.IsEmployeeOnFreezerTile(transform.position, this))
                    {
                        step = Step.AtFreezer;
                        stateTimer = 0f;
                    }
                    else
                    {
                        path.Clear();
                        pathDestination = Vector3.zero;
                    }
                }
                break;

            case Step.AtFreezer:
                if (!manager.IsEmployeeOnFreezerTile(transform.position, this))
                {
                    step = Step.GoToFreezer;
                    path.Clear();
                    pathDestination = Vector3.zero;
                    break;
                }
                stateTimer += Time.deltaTime;
                float freezerTime = manager.GetFreezerInteractionTime(this);
                ShowTaskBar = true;
                TaskProgress = Mathf.Clamp01(stateTimer / freezerTime);
                if (stateTimer >= freezerTime)
                {
                    hasPatty = true;
                    step = Step.GoToGrill;
                    stateTimer = 0f;
                }
                break;

            case Step.GoToGrill:
                if (MoveToward(manager.GetGrillPosition(this)))
                {
                    if (manager.IsEmployeeOnGrillTile(transform.position, this))
                    {
                        if (hasPatty && manager.PlacePattyOnGrill(this))
                        {
                            hasPatty = false;
                            step = Step.AtGrill;
                            stateTimer = 0f;
                        }
                        else
                        {
                            path.Clear();
                            pathDestination = Vector3.zero;
                        }
                    }
                    else
                    {
                        path.Clear();
                        pathDestination = Vector3.zero;
                    }
                }
                break;

            case Step.AtGrill:
                if (!manager.IsEmployeeOnGrillTile(transform.position, this))
                {
                    step = Step.GoToGrill;
                    path.Clear();
                    pathDestination = Vector3.zero;
                    break;
                }
                stateTimer += Time.deltaTime;
                float grillTotal = manager.GetGrillPlaceTime(this) + manager.GetGrillCookTime(this) + manager.GetGrillWaitAfterCookedTime(this) + manager.GetGrillTakeTime(this);
                ShowTaskBar = true;
                TaskProgress = Mathf.Clamp01(stateTimer / grillTotal);
                if (stateTimer >= grillTotal && manager.TakePattyFromGrill(this))
                {
                    hasPatty = true;
                    step = Step.GoToPantry;
                    stateTimer = 0f;
                }
                break;

            case Step.GoToPantry:
                if (!GetNextPantryItem(out ItemDefinition nextItem))
                {
                    step = Step.GoToAssembly;
                    break;
                }
                if (MoveToward(manager.GetPantryPosition(nextItem, this)))
                {
                    if (manager.IsEmployeeOnPantryTile(transform.position, this))
                    {
                        step = Step.AtPantry;
                        stateTimer = 0f;
                    }
                    else
                    {
                        path.Clear();
                        pathDestination = Vector3.zero;
                    }
                }
                break;

            case Step.AtPantry:
                if (!manager.IsEmployeeOnPantryTile(transform.position, this))
                {
                    step = Step.GoToPantry;
                    path.Clear();
                    pathDestination = Vector3.zero;
                    break;
                }
                stateTimer += Time.deltaTime;
                float pantryTime = manager.GetPantryInteractionTime(this);
                ShowTaskBar = true;
                TaskProgress = Mathf.Clamp01(stateTimer / pantryTime);
                if (stateTimer >= pantryTime)
                {
                    if (GetNextPantryItem(out ItemDefinition item) && manager.TakeFromPantry(item, this))
                    {
                        ingredientsHeld.Add(item);
                        step = Step.GoToPantry;
                    }
                    else
                    {
                        step = Step.GoToAssembly;
                    }
                    stateTimer = 0f;
                }
                break;

            case Step.GoToAssembly:
                if (MoveToward(manager.GetAssemblyPosition(this)))
                {
                    if (manager.IsEmployeeOnAssemblyTile(transform.position, this))
                    {
                        step = Step.AtAssembly;
                        stateTimer = 0f;
                    }
                    else
                    {
                        path.Clear();
                        pathDestination = Vector3.zero;
                    }
                }
                break;

            case Step.AtAssembly:
                if (!manager.IsEmployeeOnAssemblyTile(transform.position, this))
                {
                    step = Step.GoToAssembly;
                    path.Clear();
                    pathDestination = Vector3.zero;
                    break;
                }
                stateTimer += Time.deltaTime;
                float assemblyTime = manager.GetAssemblyInteractionTime(this);
                ShowTaskBar = true;
                TaskProgress = Mathf.Clamp01(stateTimer / assemblyTime);
                if (stateTimer >= assemblyTime)
                {
                    step = Step.GoToRegister;
                    stateTimer = 0f;
                }
                break;

            case Step.GoToRegister:
                if (currentJob != null && currentJob.register != null && MoveToward(currentJob.register.transform.position))
                    step = Step.AtRegister;
                break;

            case Step.AtRegister:
                ShowTaskBar = true;
                TaskProgress = 1f;
                if (currentJob != null && currentJob.register != null)
                {
                    currentJob.register.DeliverOrder(currentJob.order);
                    if (manager != null) manager.CompleteJob(currentJob);
                    currentJob = null;
                    step = Step.None;
                }
                break;
        }

        if (instantStepBarTimer > 0f)
        {
            instantStepBarTimer -= Time.deltaTime;
            if (instantStepBarTimer > 0f) { ShowTaskBar = true; TaskProgress = 1f; }
        }
    }

    bool GetNextPantryItem(out ItemDefinition item)
    {
        item = null;
        if (currentJob == null || currentJob.order?.lines == null) return false;
        ItemDefinition pattyItem = manager != null ? manager.PattyItem : null;
        foreach (var line in currentJob.order.lines)
        {
            if (line.item == null) continue;
            if (line.item == pattyItem) continue;
            int need = line.quantity;
            int have = 0;
            foreach (var h in ingredientsHeld)
                if (h == line.item) have++;
            if (have < need)
            {
                item = line.item;
                return true;
            }
        }
        return false;
    }

    float GroundY => groundHeight != 0f ? groundHeight : (grid != null ? grid.Origin.y : transform.position.y);

    void SnapToGround()
    {
        Vector3 p = transform.position;
        p.y = GroundY;
        transform.position = p;
    }

    bool MoveToward(Vector3 target)
    {
        if (grid == null)
            return MoveTowardStraight(target);

        if (path.Count == 0 || Vector3.SqrMagnitude(pathDestination - target) > 0.01f)
        {
            pathDestination = target;
            path = grid.GetPath(transform.position, target);
        }

        if (path.Count == 0)
            return MoveTowardStraight(target);

        Vector3 waypoint = path[0];
        waypoint.y = GroundY;
        Vector3 pos = transform.position;
        pos.y = GroundY;
        float dist = Vector3.Distance(pos, waypoint);
        if (dist < ArrivalRadius)
        {
            path.RemoveAt(0);
            if (path.Count == 0)
            {
                SnapToGround();
                return true;
            }
            waypoint = path[0];
            waypoint.y = GroundY;
        }

        Vector3 nextPos = Vector3.SmoothDamp(transform.position, waypoint, ref moveVelocity, moveSmoothTime, moveSpeed);
        nextPos.y = GroundY;
        transform.position = nextPos;
        return false;
    }

    bool MoveTowardStraight(Vector3 target)
    {
        Vector3 pos = transform.position;
        pos.y = GroundY;
        target.y = GroundY;
        float dist = Vector3.Distance(pos, target);
        if (dist < ArrivalRadius)
        {
            SnapToGround();
            return true;
        }
        Vector3 nextPos = Vector3.SmoothDamp(transform.position, target, ref moveVelocity, moveSmoothTime, moveSpeed);
        nextPos.y = GroundY;
        transform.position = nextPos;
        return false;
    }
}
