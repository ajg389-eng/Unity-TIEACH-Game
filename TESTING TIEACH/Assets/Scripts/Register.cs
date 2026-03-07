using System.Collections.Generic;
using UnityEngine;

public class Register : MonoBehaviour
{
    public bool isEnabled = true;

    [Header("Queue")]
    public Transform queueStart;      // empty object at front of the line
    public Vector3 queueDirection = Vector3.back; // direction line extends (world space)
    public float spacing = 1.2f;      // distance between customers
    public int maxQueue = 6;

    private readonly List<CustomerAI> queue = new List<CustomerAI>();
    public int QueueCount => queue.Count;

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

    // Call this when the front customer is "served"
    public void ServeFront()
    {
        if (queue.Count == 0) return;
        var front = queue[0];
        queue.RemoveAt(0);

        if (front != null) front.OnServed(); // customer leaves
        UpdateQueueTargets();
    }

    public void Toggle()
    {
        isEnabled = !isEnabled;

        if (!isEnabled)
        {
            // Optional: kick customers out when disabled (keep if you already want this)
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
            // fallback: create a capsule if you didn’t make a prefab
            cashierInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cashierInstance.transform.SetPositionAndRotation(pos, rot);
            cashierInstance.name = "Cashier";
            Destroy(cashierInstance.GetComponent<Collider>()); // so it doesn't block stuff
        }

        cashierInstance.transform.SetParent(transform);
        cashierInstance.name = "Cashier";
    }

    void Start()
    {
        if (isEnabled) SpawnCashier();
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

    // Nice visual in Scene view
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