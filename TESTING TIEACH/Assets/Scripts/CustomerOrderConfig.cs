using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Config for generating customer orders: one burger base + random ingredients (cheese, lettuce, tomato, etc.) + optional fries.
/// Create via Assets > Create > FactoryGame > Customer Order Config.
/// </summary>
[CreateAssetMenu(menuName = "FactoryGame/Customer Order Config", fileName = "CustomerOrderConfig")]
public class CustomerOrderConfig : ScriptableObject
{
    [Header("Burger")]
    [Tooltip("The base burger (patty/bun) – every order gets one")]
    public ItemDefinition burgerBase;

    [Header("Ingredients / Toppings")]
    [Tooltip("Possible toppings – each customer gets a random selection (e.g. cheese, lettuce, tomato)")]
    public List<ItemDefinition> ingredientOptions = new List<ItemDefinition>();
    [Tooltip("Minimum number of ingredients on the burger (0 = plain allowed)")]
    public int minIngredients = 1;
    [Tooltip("Maximum number of ingredients on the burger")]
    public int maxIngredients = 3;

    [Header("Sides")]
    [Tooltip("Added to every order (e.g. Fries)")]
    public ItemDefinition friesItem;

    public CustomerOrder GenerateRandomOrder()
    {
        var order = new CustomerOrder();

        if (burgerBase != null)
            order.lines.Add(new CustomerOrder.OrderLine(burgerBase, 1));

        if (ingredientOptions != null && ingredientOptions.Count > 0)
        {
            int count = Mathf.Clamp(Random.Range(minIngredients, maxIngredients + 1), 0, ingredientOptions.Count);
            var shuffled = new List<ItemDefinition>(ingredientOptions);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var t = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = t;
            }
            for (int i = 0; i < count; i++)
            {
                if (shuffled[i] != null)
                    order.lines.Add(new CustomerOrder.OrderLine(shuffled[i], 1));
            }
        }

        if (friesItem != null)
            order.lines.Add(new CustomerOrder.OrderLine(friesItem, 1));

        return order;
    }
}
