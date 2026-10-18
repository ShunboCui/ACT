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

        // Reduce count
        foreach (var pair in materialUsage)
        {
            InventorySlotItem item =
                materials.Find(x => x.uid == pair.Key);

            item.num -= pair.Value;
        }

        // Remove all items with amount 0
        materials.RemoveAll(x => x.num <= 0);

        return true;
    }

    public void ConfirmWeaponEnhance(string weaponUid, int exp)
    {
        GameManager.Instance.GetInventoryItemByUId(weaponUid).exp += exp;
    }
}
