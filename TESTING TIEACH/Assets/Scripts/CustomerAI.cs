using UnityEngine;

public class CustomerAI : MonoBehaviour
{
    public float moveSpeed = 2.5f;

    private Register reg;
    private Vector3 targetPos;
    private bool hasTarget;

    private bool isFront;

    float queueJoinTime;
    public float QueueJoinTime => queueJoinTime;

    CustomerOrder order;
    CustomerOrderLabel orderLabel;

    public CustomerOrder GetOrder() => order;

    public void SetOrder(CustomerOrder o)
    {
        order = o;
        if (orderLabel == null) orderLabel = GetComponent<CustomerOrderLabel>();
        if (orderLabel == null) orderLabel = gameObject.AddComponent<CustomerOrderLabel>();
        string label = (order != null && order.lines != null && order.lines.Count > 0) ? order.GetDisplayString() : "No order";
        orderLabel.Setup(transform, label);
    }

    public void SetQueueJoinTime(float time)
    {
        queueJoinTime = time;
    }

    public void SetTargetRegister(Register r)
    {
        reg = r;
        if (reg == null) return;

        reg.TryJoinQueue(this);
    }

    public void SetQueueSlot(Register register, Vector3 slotPos, bool front)
    {
        reg = register;
        targetPos = slotPos;
        isFront = front;
        hasTarget = true;
    }

    bool leaving;
    Vector3 exitTarget;

    public void OnServed(Transform exit)
    {
        reg = null;
        hasTarget = true;
        leaving = true;
        exitTarget = exit != null ? exit.position : transform.position + transform.forward * 10f;
        targetPos = exitTarget;
    }

    public void OnRegisterDisabled()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (!hasTarget) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (leaving && Vector3.Distance(transform.position, targetPos) < 0.2f)
            Destroy(gameObject);
    }
}
