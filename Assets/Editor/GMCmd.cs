using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GMCmd
{
    [MenuItem("GMCmd/Read table")]
    public static void ReadTable()
    {
        PackageTable packageTable = Resources.Load<PackageTable>("TableData/PackageTable");
        foreach (PackageTableItem packageTableitem in packageTable.DataList)
        {
            Debug.Log(packageTableitem);
        }
    }

    [MenuItem("GMCmd/Create packate data")]
    public static void CreateLocalPackageData()
    {
        PackageLocalData.Instance.items = new List<PackageLocalItem>();
        for (int i = 1; i < 6; i ++) {
            PackageLocalItem packageLocalItem = new PackageLocalItem()
            {
                uid = Guid.NewGuid().ToString(),
                id = i,
                num = 1,
                level = 1,
                isNew = i % 2 == 1
            };
            PackageLocalData.Instance.items.Add(packageLocalItem);
        }
        PackageLocalData.Instance.SavePackage();
    }

    [MenuItem("GMCmd/Read package data")]
    public static void ReadLocalPackageData()
    {
        List<PackageLocalItem> packageLocalItem = new List<PackageLocalItem>();
        packageLocalItem = PackageLocalData.Instance.LoadPackage();
        foreach (PackageLocalItem item in packageLocalItem)
        {
            Debug.Log(item);
        }
    }

    [MenuItem("GMCmd/Open package panel")]
    public static void OpenPackagePanel()
    {
        UIManager.Instance.OpenPanel(UIConst.PackagePanel);

    }

    [MenuItem("GMCmd/Switch to weapon 2")]
    public static void SwitchWeapon()
    {
        WeaponController.Instance.EquipWeapon(GameObject.Find("Player").GetComponent<PlayerControl>().Lions_Roar_Data);
    }
}
