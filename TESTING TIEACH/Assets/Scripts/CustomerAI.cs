using UnityEngine;

public class CustomerAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;

    private Register reg;
    private Vector3 targetPos;
    private bool hasTarget;

    // For “front of line” behavior later
    private bool isFront;

    public void SetTargetRegister(Register r)
    {
        reg = r;
        if (reg == null) return;

        // Register assigns the actual slot position when joining queue
        reg.TryJoinQueue(this);
    }

    public void SetQueueSlot(Register register, Vector3 slotPos, bool front)
    {
        reg = register;
        targetPos = slotPos;
        isFront = front;
        hasTarget = true;
    }

    public void OnServed()
    {
        // Simple: leave (later you can walk to exit)
        Destroy(gameObject);
    }

    public void OnRegisterDisabled()
    {
        // Simple: leave if register shuts off
        Destroy(gameObject);
    }

    void Update()
    {
        if (!hasTarget) return;

        // Move to assigned slot
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // If you want service to happen automatically when the front reaches the front slot:
        if (isFront && Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            // Do nothing by default; service can be triggered by a timer elsewhere.
        }
    }
}