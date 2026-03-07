using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple timer-based order completion. Fully disabled when ProductionManager exists –
/// customers only get served when employees deliver orders (freezer -> grill -> pantry -> assembly -> register).
/// </summary>
public class Kitchen : MonoBehaviour
{
    [Header("Registers")]
    public List<Register> registers = new List<Register>();

    [Header("Prep")]
    [Tooltip("Seconds to create/assemble one order (only used when no ProductionManager in scene)")]
    public float prepTimePerOrder = 5f;

    float prepTimer;
    CustomerOrder orderInProgress;
    Register targetRegister;

    void Update()
    {
        if (ProductionManager.Instance != null)
            return;

        if (orderInProgress != null && targetRegister != null)
        {
            prepTimer -= Time.deltaTime;
            if (prepTimer <= 0f)
            {
                targetRegister.DeliverOrder(orderInProgress);
                orderInProgress = null;
                targetRegister = null;
            }
            return;
        }

        foreach (var reg in registers)
        {
            if (reg == null || !reg.isEnabled) continue;
            if (reg.HasPreparedOrder) continue;

            CustomerOrder frontOrder = reg.GetFrontCustomerOrder();
            if (frontOrder == null || frontOrder.lines == null || frontOrder.lines.Count == 0)
                continue;

            orderInProgress = frontOrder.Clone();
            targetRegister = reg;
            prepTimer = prepTimePerOrder;
            return;
        }
    }
}
