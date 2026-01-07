using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct LootItem
{
    public ItemData itemData;
    public uint tokenId;
    [Range(0, 100)]
    public float dropChance;
}

[CreateAssetMenu(fileName = "New Item Arena Reward Manager", menuName = "ItemDrop/Arena_0 Reward Manager")]
public class ItemDropManager : ScriptableObject
{
    public List<LootItem> items;
    public LootItem GetRandomLoot()
    {
        float totalWeight = 0;
        foreach (var item in items) totalWeight += item.dropChance;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in items)
        {
            currentWeight += item.dropChance;
            if (randomValue <= currentWeight)
            {
                Debug.Log($"Random result: {item.itemData.Id}");
                return item;
            }
        }
        return items[0];
    }
}