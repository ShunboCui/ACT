using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController
{
    public InventoryController()
    {

    }

    public List<InventorySlotItem> GetItemsByType(int itemType)
    {
        return GameManager.Instance.GetInventorySlotItemsByType(itemType);
    }

    public bool DeleteItems(List<string> selectedDeleteUid)
    {
        if (selectedDeleteUid == null || selectedDeleteUid.Count == 0)
            return false;
        GameManager.Instance.DeletePackageItems(selectedDeleteUid);
        selectedDeleteUid.Clear();
        return true;
    }

    public bool ConsumeMaterials(
    Dictionary<string, int> materialUsage)
    {
        List<InventorySlotItem> materials = GetItemsByType(GameConst.InventoryMaterialType);

        // 第一步：先检查所有材料是否足够
        foreach (var pair in materialUsage)
        {
            string uid = pair.Key;
            int count = pair.Value;

            if (count <= 0)
                return false;

            InventorySlotItem item =
                materials.Find(x => x.uid == uid);

            if (item == null || item.num < count)
                return false;
        }

        // 第二步：检查通过后，统一扣除数量
        foreach (var pair in materialUsage)
        {
            InventorySlotItem item =
                materials.Find(x => x.uid == pair.Key);

            item.num -= pair.Value;
        }

        // 第三步：移除数量为 0 的物品
        materials.RemoveAll(x => x.num <= 0);

        return true;
    }
}
