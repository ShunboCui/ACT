using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    private PackageTable packageTable;
    bool sort;
    public GameObject[] enemyList;
    public GameObject winUI;
    public bool IsGamePaused;
    private bool win = false;

    public static GameManager Instance {
        get {
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //UIManager.Instance.OpenPanel(UIConst.Background);
        LockMouse();
        this.enemyList = GameObject.FindGameObjectsWithTag("Enemy");
    }

    void Update()
    {
        MouseControl();
        if (Input.GetKeyDown(KeyCode.B))
        {
            UIManager.Instance.OpenPanel("MainPanel");
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            IsGamePaused = true;
        }

        WinUIControl();
        enemyList = GameObject.FindGameObjectsWithTag("Enemy");
    }

    public PackageTable GetPackageTable()
    {
        if (packageTable == null)
        {
            packageTable = Resources.Load<PackageTable>("TableData/PackageTable");
        }
        return packageTable;
    }

    /*
    public List<InventorySlotItem> GetLocalData()
    {
        List<InventorySlotItem> myList;
        myList = InventorySlotData.Instance.LoadInventory();
        return myList;
    }*/

    public PackageTableItem GetPackageTableItemById(int id)
    {
        List<PackageTableItem> DataList = GetPackageTable().DataList;
        foreach (PackageTableItem DataItem in DataList)
        {
            if (DataItem.id == id)
            {
                return DataItem;
            }
        }
        return null;
    }

    public InventorySlotItem GetInventoryItemByUId(string uid)
    {
        List<InventorySlotItem> DataList = GetInventorySaveData();
        foreach (InventorySlotItem DataItem in DataList)
        {
            if (DataItem.uid == uid)
            {
                return DataItem;
            }
        }
        return null;
    }

    public List<InventorySlotItem> GetInventorySaveData()
    {
        return InventorySlotData.Instance.LoadInventory();
    }

    public List<InventorySlotItem> GetSortedPackageLocalItems()
    {
        List<InventorySlotItem> localItems = new List<InventorySlotItem>();
        localItems = InventorySlotData.Instance.LoadInventory();
        localItems.Sort(new InventoryItemComparer());
        return localItems;

    }

    public List<PackageTableItem> GetPackageTableByType(int itemType)
    {
        List<PackageTableItem> packageItems = new List<PackageTableItem>();
        foreach (PackageTableItem packageItem in Instance.GetPackageTable().DataList)
        {
            if (packageItem.type == itemType)
            {
                packageItems.Add(packageItem);
            }
        }
        return packageItems;
    }

    public List<InventorySlotItem> GetInventorySlotItemsByType(int itemType)
    {
        List<PackageTableItem> tableItems = GetPackageTableByType(itemType);
        List<int> typeIds = new List<int>();
        for (int i = 0; i < tableItems.Count; i++)
        {
            typeIds.Add(tableItems[i].id);
        }

        List <InventorySlotItem> localItems = GetInventorySaveData();
        List<InventorySlotItem> res = new List<InventorySlotItem>();
        for (int i = 0; i < localItems.Count; i++) {
            if (typeIds.Contains(localItems[i].id)) {
                res.Add(localItems[i]); 
            }
        }

        return res;
    }

    public InventorySlotItem PullRandom()
    {
        List<PackageTableItem> packageItems = GetPackageTableByType(GameConst.InventoryWeaponType);
        int index = Random.Range(0, packageItems.Count);
        PackageTableItem randomItem = packageItems[index];
        InventorySlotItem packageLocalItem = new InventorySlotItem() {
            uid = System.Guid.NewGuid().ToString(),
            id = randomItem.id,
            num = 1,
            exp = 0,
            isNew = true
        };
        InventorySlotData.Instance.items.Add(packageLocalItem);
        InventorySlotData.Instance.SaveInventory();
        return packageLocalItem;
    }

    public List<InventorySlotItem> PullRandom10()
    {
        List<InventorySlotItem> packageLocalItems = new List<InventorySlotItem>();
        for (int i = 0; i < 10; i++)
        {
            InventorySlotItem packageLocalItem = PullRandom();
            packageLocalItems.Add(packageLocalItem);
        }
        if (!sort)
        {
            packageLocalItems.Sort(new InventoryItemComparer());
        }
        return packageLocalItems;
    }

    public void DeletePackageItems(List<string> uids)
    {
        foreach (string uid in uids)
        {
            DeletePackageItem(uid);
        }
        InventorySlotData.Instance.SaveInventory();
    }

    public void DeletePackageItem(string uid) {
        InventorySlotItem deleteItem = GameManager.Instance.GetInventoryItemByUId(uid);
        if (deleteItem == null)
        {
            return;
        }
        InventorySlotData.Instance.items.Remove(deleteItem);
    }

    public void ModifyInventoryItemsAmount(Dictionary<string, int> itemPairs)
    {
        foreach (string uid in itemPairs.Keys)
        {
            ModifyInventoryItemAmount(uid, itemPairs[uid]);
        }
    }

    public void ModifyInventoryItemAmount(string uid, int amount)
    {
        InventorySlotItem inventoryItem = InventorySlotData.Instance.items.Find(x => x.uid == uid);
        inventoryItem.num -= amount;
    }

    public void MouseControl()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            UnlockMouse();
        }

        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            LockMouse();
        }
    }

    public void LockMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void GameOver()
    {
        UnlockMouse();
        UIManager.Instance.OpenPanel("DeadMenu");

        Time.timeScale = 0f;
    }

    private void WinUIControl()
    {
        //Debug.Log(enemyList.Length.ToString());
        if (!win && enemyList.Length == 0)
        {
            win = true;
            Invoke(nameof(EnableWinUI), 2.0f);
        }
    }

    
    private void EnableWinUI()
    {
        UnlockMouse();
        UIManager.Instance.OpenPanel("VictoryMenu");
        Time.timeScale = 0f;
    }
}

public class InventoryItemComparer : IComparer<InventorySlotItem>
{
    public int Compare(InventorySlotItem a, InventorySlotItem b)
    {
        PackageTableItem x = GameManager.Instance.GetPackageTableItemById(a.id);
        PackageTableItem y = GameManager.Instance.GetPackageTableItemById(b.id);

        int starComparison = y.star.CompareTo(x.star);

        if (starComparison == 0)
        {
            int lvlComparison = b.level.CompareTo(a.level);

            if (lvlComparison == 0)
            {
                return y.id.CompareTo(x.id);
            }

            return lvlComparison;
        }

        return starComparison;
    }
}

public class GameConst
{
    public const int InventoryWeaponType = 0;

    public const int InventoryFoodType = 1;
    public const int InventoryMaterialType = 2;
}