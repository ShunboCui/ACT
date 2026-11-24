using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LootEntry
{
    [Header("Item")]
    public int itemId;

    [Header("Drop Settings")]
    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Min(1)]
    public int minAmount = 1;

    [Min(1)]
    public int maxAmount = 1;

    public bool TryRoll(out int amount)
    {
        amount = 0;

        if (dropChance <= 0f)
            return false;

        if (dropChance < 1f &&
            UnityEngine.Random.value >= dropChance)
            return false;

        amount = UnityEngine.Random.Range(
            minAmount,
            Mathf.Max(minAmount, maxAmount) + 1
        );

        return true;
    }
}

public struct DropResult
{
    public PackageTableItem item;
    public int amount;

    public DropResult(PackageTableItem item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

[CreateAssetMenu(
    menuName = "PackageProject/MonsterLootTable",
    fileName = "MonsterLootTable"
)]
public class MonsterLootTableSO : ScriptableObject
{
    [Header("Item Database")]
    [SerializeField] private PackageTable packageTable;

    [Header("Loot Entries")]
    [SerializeField]
    public List<LootEntry> entries =
        new List<LootEntry>();

    public List<DropResult> RollDrops()
    {
        List<DropResult> results = new List<DropResult>();

        if (packageTable == null)
        {
            Debug.LogError("PackageTable is missing!");
            return results;
        }

        foreach (LootEntry entry in entries)
        {
            PackageTableItem item =
                GameManager.Instance.GetPackageTableItemById(entry.itemId);

            if (item == null)
            {
                Debug.LogWarning(
                    $"Cannot find item ID: {entry.itemId}"
                );
                continue;
            }

            if (entry.TryRoll(out int amount))
            {
                results.Add(new DropResult(item, amount));
            }
        }

        return results;
    }
}