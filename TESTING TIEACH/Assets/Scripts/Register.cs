using System.Collections.Generic;
using UnityEngine;

public class Register : MonoBehaviour
{
    public bool isEnabled = true;

    [Header("Queue")]
    public Transform queueStart;      // empty object at front of the line
    public Vector3 queueDirection = Vector3.back; // direction line extends (world space)
    public float spacing = 1.2f;      // distance between customers
    [Tooltip("Front customer must be this close to the register before they can be served")]
    public float serveArrivalRadius = 0.6f;
    public int maxQueue = 6;

    private readonly List<CustomerAI> queue = new List<CustomerAI>();
    public int QueueCount => queue.Count;

    [Header("Prepared orders")]
    [Tooltip("Set by Kitchen when order is assembled and delivered")]
    CustomerOrder preparedOrder;
    [Tooltip("Where customers walk to after being served")]
    public Transform storeExit;

    [Header("Cashier")]
    public Transform cashierSpawnPoint;   // empty child behind register
    public GameObject cashierPrefab;      // assign a capsule prefab (or create at runtime)
    private GameObject cashierInstance;

    public bool HasSpace()
    {
        return isEnabled && queueStart != null && queue.Count < maxQueue;
    }

    public bool TryJoinQueue(CustomerAI customer)
    {
        if (!HasSpace() || customer == null) return false;
        if (queue.Contains(customer)) return true;

        customer.SetQueueJoinTime(Time.time);
        queue.Add(customer);
        UpdateQueueTargets();
        return true;
    }

    public void LeaveQueue(CustomerAI customer)
    {
        if (customer == null) return;
        queue.Remove(customer);
        UpdateQueueTargets();
    }

    public bool HasPreparedOrder => preparedOrder != null && preparedOrder.lines != null && preparedOrder.lines.Count > 0;

    public CustomerOrder GetFrontCustomerOrder()
    {
        if (queue.Count == 0) return null;
        var front = queue[0];
        return front != null ? front.GetOrder() : null;
    }

    /// <summary>Kitchen calls this when an order is assembled and delivered to this register. Serves the front customer immediately if their order matches.</summary>
    public void DeliverOrder(CustomerOrder order)
    {
        preparedOrder = order != null ? order.Clone() : null;
        TryServeFront();
    }

    /// <summary>Serve front customer only when we have a matching prepared order and they have arrived at the register.</summary>
    public void TryServeFront()
    {
        if (queue.Count == 0 || queueStart == null) return;
        var front = queue[0];
        if (front == null) return;

        Vector3 frontSlotPos = queueStart.position + queueDirection.normalized * 0f;
        float dist = Vector3.Distance(front.transform.position, frontSlotPos);
        if (dist > serveArrivalRadius)
            return;

        CustomerOrder order = front.GetOrder();
        if (order == null || order.lines == null || order.lines.Count == 0)
            return;
        if (preparedOrder == null || !order.Matches(preparedOrder))
            return;
        preparedOrder = null;
        CompleteServeFront(front);
    }

    void CompleteServeFront(CustomerAI front)
    {
        if (queue.Count == 0 || queue[0] != front) return;
        queue.RemoveAt(0);

        if (StoreStatisticsManager.Instance != null)
            StoreStatisticsManager.Instance.RecordOrderCompleted(front.QueueJoinTime, this);
        if (front != null) front.OnServed(storeExit);
        UpdateQueueTargets();
    }

    public void Toggle()
    {
        isEnabled = !isEnabled;

        if (!isEnabled)
        {
            foreach (var c in queue)
                if (c != null) c.OnRegisterDisabled();
            queue.Clear();

            RemoveCashier();
        }
        else
        {
            SpawnCashier();
        }

        UpdateQueueTargets();
    }

    void SpawnCashier()
    {
        if (cashierInstance != null) return;

        Vector3 pos = cashierSpawnPoint ? cashierSpawnPoint.position : transform.position + transform.forward * -0.8f;
        Quaternion rot = cashierSpawnPoint ? cashierSpawnPoint.rotation : transform.rotation;

        if (cashierPrefab != null)
        {
            cashierInstance = Instantiate(cashierPrefab, pos, rot);
        }
        else
        {
            cashierInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cashierInstance.transform.SetPositionAndRotation(pos, rot);
            cashierInstance.name = "Cashier";
            Destroy(cashierInstance.GetComponent<Collider>());
        }

        cashierInstance.transform.SetParent(transform);
        cashierInstance.name = "Cashier";
    }

    void Start()
    {
        if (isEnabled) SpawnCashier();
    }

    void Update()
    {
        if (isEnabled)
            TryServeFront();
    }

    void RemoveCashier()
    {
        if (cashierInstance == null) return;
        Destroy(cashierInstance);
        cashierInstance = null;
    }

    void UpdateQueueTargets()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (c == null) continue;

            Vector3 dir = queueDirection.normalized;
            Vector3 slotPos = queueStart.position + dir * (spacing * i);
            c.SetQueueSlot(this, slotPos, i == 0);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!queueStart) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < maxQueue; i++)
        {
            Vector3 slot = queueStart.position + queueDirection.normalized * (spacing * i);
            Gizmos.DrawSphere(slot, 0.15f);
        }
    }
}
