using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hired employee that walks through production: freezer -> grill -> pantry -> assembly -> register.
/// Uses the grid to pathfind; only walks on empty cells and cannot clip through placed objects.
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

    ProductionJob? currentJob;

    public bool IsIdle => !currentJob.HasValue;
    public bool HasJob => currentJob.HasValue;
    float stateTimer;

    List<Vector3> path = new List<Vector3>();
    Vector3 pathDestination;
    Vector3 moveVelocity;
    const float ArrivalRadius = 0.25f;

    enum Step
    {
        None,
        GoToFreezer, AtFreezer,
        GoToGrill, AtGrillPlace, AtGrillWaitCook, AtGrillTake,
        GoToPantry, AtPantry,
        GoToAssembly, AtAssembly,
        GoToRegister, AtRegister
    }

    Step step;
    bool hasPatty;
    readonly List<ItemDefinition> ingredientsHeld = new List<ItemDefinition>();

    ProductionManager manager;

    void Start()
    {
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
        if (currentJob.HasValue) return;
        currentJob = job;
        step = Step.GoToFreezer;
        stateTimer = 0f;
        hasPatty = false;
        ingredientsHeld.Clear();
    }

    void Update()
    {
        SnapToGround();
        if (!currentJob.HasValue || manager == null) return;
        RunWorkflow();
    }

    void RunWorkflow()
    {
        switch (step)
        {
            case Step.GoToFreezer:
                if (MoveToward(manager.GetFreezerPosition()))
                    step = Step.AtFreezer;
                break;

            case Step.AtFreezer:
                hasPatty = true;
                step = Step.GoToGrill;
                break;

            case Step.GoToGrill:
                if (MoveToward(manager.GetGrillPosition()))
                    step = Step.AtGrillPlace;
                break;

            case Step.AtGrillPlace:
                if (manager.PlacePattyOnGrill())
                {
                    hasPatty = false;
                    step = Step.AtGrillWaitCook;
                    stateTimer = 0f;
                }
                break;

            case Step.AtGrillWaitCook:
                stateTimer += Time.deltaTime;
                if (manager.IsGrillCooked() && stateTimer >= 0.5f)
                    step = Step.AtGrillTake;
                break;

            case Step.AtGrillTake:
                if (manager.TakePattyFromGrill())
                {
                    hasPatty = true;
                    step = Step.GoToPantry;
                }
                break;

            case Step.GoToPantry:
                if (!GetNextPantryItem(out ItemDefinition nextItem))
                {
                    step = Step.GoToAssembly;
                    break;
                }
                if (MoveToward(manager.GetPantryPosition(nextItem)))
                    step = Step.AtPantry;
                break;

            case Step.AtPantry:
                if (GetNextPantryItem(out ItemDefinition item) && manager.TakeFromPantry(item))
                {
                    ingredientsHeld.Add(item);
                    step = Step.GoToPantry;
                }
                else
                {
                    step = Step.GoToAssembly;
                }
                break;

            case Step.GoToAssembly:
                if (MoveToward(manager.GetAssemblyPosition()))
                    step = Step.AtAssembly;
                break;

            case Step.AtAssembly:
                stateTimer += Time.deltaTime;
                if (stateTimer >= 0.8f)
                {
                    step = Step.GoToRegister;
                    stateTimer = 0f;
                }
                break;

            case Step.GoToRegister:
                if (currentJob.HasValue && currentJob.Value.register != null && MoveToward(currentJob.Value.register.transform.position))
                    step = Step.AtRegister;
                break;

            case Step.AtRegister:
                if (currentJob.HasValue && currentJob.Value.register != null)
                {
                    currentJob.Value.register.DeliverOrder(currentJob.Value.order);
                    currentJob = null;
                    step = Step.None;
                }
                break;
        }
    }

    bool GetNextPantryItem(out ItemDefinition item)
    {
        item = null;
        if (!currentJob.HasValue || currentJob.Value.order?.lines == null) return false;
        ItemDefinition pattyItem = manager != null ? manager.PattyItem : null;
        foreach (var line in currentJob.Value.order.lines)
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
