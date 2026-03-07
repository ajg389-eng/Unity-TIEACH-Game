using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public float spawnInterval = 3f;

    [Tooltip("Assign to give customers random burger + fries orders")]
    public CustomerOrderConfig orderConfig;

    public List<Register> registers = new List<Register>();

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        Register r = GetBestRegister();
        if (r == null) return;

        var c = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        var ai = c.GetComponent<CustomerAI>();

        if (ai != null)
        {
            if (orderConfig != null)
                ai.SetOrder(orderConfig.GenerateRandomOrder());
            else
            {
                ai.SetOrder(new CustomerOrder());
                if (Time.frameCount % 60 == 0) Debug.LogWarning("CustomerSpawner: Assign Order Config so customers get burger + ingredients. Showing 'No order'.");
            }
            ai.SetTargetRegister(r);
        }
    }

    Register GetBestRegister()
    {
        Register best = null;
        int bestCount = int.MaxValue;

        foreach (var r in registers)
        {
            if (r == null) continue;
            if (!r.HasSpace()) continue;

            int count = r.QueueCount;
            if (count < bestCount)
            {
                bestCount = count;
                best = r;
            }
        }

        return best;
    }
}