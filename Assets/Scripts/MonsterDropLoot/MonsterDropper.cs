using UnityEngine;

public class MonsterDropper : MonoBehaviour
{
    [SerializeField] private MonsterLootTableSO lootTable;
    [SerializeField] private Transform dropOrigin;

    [SerializeField] private float scatterRadius = 0.5f;

    public void DropLoot()
    {
        if (lootTable == null)
            return;

        Vector3 origin = dropOrigin != null
            ? dropOrigin.position
            : transform.position;

        var results = lootTable.RollDrops();

        foreach (DropResult result in results)
        {
            // Get model path from PackageTableItem
            GameObject prefab = Resources.Load<GameObject>(
                result.item.modelPath
            );

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Cannot load item prefab: {result.item.modelPath}"
                );
                continue;
            }

            Vector2 offset =
                Random.insideUnitCircle * scatterRadius;

            Vector3 position = origin +
                new Vector3(offset.x, 0.25f, offset.y);

            GameObject instance = Instantiate(
                prefab,
                position,
                Quaternion.identity
            );

            WorldItemPickup pickup =
                instance.GetComponent<WorldItemPickup>();

            if (pickup == null)
            {
                Debug.LogError(
                    $"{prefab.name} missing WorldItemPickup!"
                );
                Destroy(instance);
                continue;
            }

            pickup.Initialize(result.item.id, result.amount);
        }
    }
}