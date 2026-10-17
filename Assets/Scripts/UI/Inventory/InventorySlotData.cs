using System.Collections.Generic;
using UnityEngine;

public class InventorySlotData
{
    private InventorySlotData()
    {
        // Optional initialization, for example: items = new List<PackageLocalItem>();
    }

    private static InventorySlotData _instance;

    public static InventorySlotData Instance {
        get { 
            if (_instance == null)
            {
                _instance = new InventorySlotData();
            }
            return _instance; 
        }
    }

    public List<InventorySlotItem> items;

    public void SaveInventory()
    {
        string inventoryJson = JsonUtility.ToJson(this);
        PlayerPrefs.SetString("InventorySaveData", inventoryJson);
        PlayerPrefs.Save();
    }

    public List<InventorySlotItem> LoadInventory()
    {
        if (items != null)
        {
            return items;
        }
        if (PlayerPrefs.HasKey("InventorySaveData"))
        {
            string inventoryJson = PlayerPrefs.GetString("InventorySaveData");
            InventorySlotData packageLocalData = JsonUtility.FromJson<InventorySlotData>(inventoryJson);
            items = packageLocalData.items;
            return items;
        }
        else
        {
            items = new List<InventorySlotItem>();
            return items;
        }
    }
}

[System.Serializable]
public class InventorySlotItem
{
    public string uid;
    public int id;
    public int num;
    public int level {
        get { return getLevel(exp); }
    }
    public int exp;
    public bool isNew;
    public override string ToString()
    {
        return string.Format("[id]:{0} [num]:{1}", id, num);
    }

    private int getLevel(int exp)
    {
        int res = 1;
        res += (int)(exp / 1000);
        return res;
    }
}