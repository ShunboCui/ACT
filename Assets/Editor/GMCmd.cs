using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public class GMCmd
{
    [MenuItem("GMCmd/Print table")]
    public static void PrintTable()
    {
        PackageTable packageTable = Resources.Load<PackageTable>("TableData/PackageTable");
        foreach (PackageTableItem packageTableitem in packageTable.DataList)
        {
            Debug.Log(packageTableitem);
        }
    }

    [MenuItem("GMCmd/Create inventory save data")]
    public static void CreateInventorySaveData()
    {
        InventorySlotData.Instance.items = new List<InventorySlotItem>();
        for (int i = 1; i < 6; i ++) {
            InventorySlotItem packageLocalItem = new InventorySlotItem()
            {
                uid = Guid.NewGuid().ToString(),
                id = i,
                num = 1,
                exp = 0,
                isNew = i % 2 == 1
            };
            InventorySlotData.Instance.items.Add(packageLocalItem);
        }

        for (int j = 6; j < 9; j++)
        {
            InventorySlotItem packageLocalItemMaterial = new InventorySlotItem()
            {
                uid = Guid.NewGuid().ToString(),
                id = j,
                num = 50,
                exp = 0,
                isNew = false
            };
            InventorySlotData.Instance.items.Add(packageLocalItemMaterial);
        }

        InventorySlotData.Instance.SaveInventory();
    }

    [MenuItem("GMCmd/Print inventory data")]
    public static void PrintInventorySaveData()
    {
        List<InventorySlotItem> inventorySlotItems = new List<InventorySlotItem>();
        inventorySlotItems = InventorySlotData.Instance.LoadInventory();
        foreach (InventorySlotItem item in inventorySlotItems)
        {
            Debug.Log(item);
        }
    }

    [MenuItem("GMCmd/Open package panel")]
    public static void OpenInventoryPanel()
    {
        UIManager.Instance.OpenPanel(UIConst.InventoryPanel);

    }

    [MenuItem("GMCmd/Switch to weapon 2")]
    public static void SwitchWeapon()
    {
        //WeaponController.Instance.EquipWeapon(GameObject.Find("Player").GetComponent<PlayerControl>().Lions_Roar_Data);
        PackageTableItem packageTableItem = GameManager.Instance.GetPackageTableItemById(5);
        WeaponController.Instance.EquipWeapon(packageTableItem);
    }

    [MenuItem("GMCmd/Stop camera")]
    public static void StopCamera()
    {
        GameManager.Instance.StopCamera();
    }

    [MenuItem("GMCmd/Start camera")]
    public static void StartCamera()
    {
        GameManager.Instance.StartCamera();
    }
}
