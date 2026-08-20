using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    private PackageTable packageTable;
    bool sort;

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
        }
    }

    public PackageTable GetPackageTable()
    {
        if (packageTable == null)
        {
            packageTable = Resources.Load<PackageTable>("TableData/PackageTable");
        }
        return packageTable;
    }

    public List<PackageLocalItem> GetLocalData()
    {
        List<PackageLocalItem> myList;
        myList = PackageLocalData.Instance.LoadPackage();
        return myList;
    }

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
    public PackageLocalItem GetPackageLocalItemByUId(string uid)
    {
        List<PackageLocalItem> DataList = GetPackageLocalItems();
        foreach (PackageLocalItem DataItem in DataList)
        {
            if (DataItem.uid == uid)
            {
                return DataItem;
            }
        }
        return null;
    }

    public List<PackageLocalItem> GetPackageLocalItems()
    {
        return PackageLocalData.Instance.LoadPackage();
    }

    public List<PackageLocalItem> GetSortedPackageLocalItems()
    {
        List<PackageLocalItem> localItems = new List<PackageLocalItem>();
        localItems = PackageLocalData.Instance.LoadPackage();
        localItems.Sort(new PackageItemComparer());
        return localItems;

    }

    public List<PackageTableItem> GetPackageDataByType(int itemType)
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

    public PackageLocalItem PullRandom()
    {
        List<PackageTableItem> packageItems = GetPackageDataByType(GameConst.InventoryWeaponType);
        int index = Random.Range(0, packageItems.Count);
        PackageTableItem randomItem = packageItems[index];
        PackageLocalItem packageLocalItem = new PackageLocalItem() {
            uid = System.Guid.NewGuid().ToString(),
            id = randomItem.id,
            num = 1,
            level = 1,
            isNew = true
        };
        PackageLocalData.Instance.items.Add(packageLocalItem);
        PackageLocalData.Instance.SavePackage();
        return packageLocalItem;
    }

    public List<PackageLocalItem> PullRandom10()
    {
        List<PackageLocalItem> packageLocalItems = new List<PackageLocalItem>();
        for (int i = 0; i < 10; i++)
        {
            PackageLocalItem packageLocalItem = PullRandom();
            packageLocalItems.Add(packageLocalItem);
        }
        if (!sort)
        {
            packageLocalItems.Sort(new PackageItemComparer());
        }
        return packageLocalItems;
    }

    public void DeletePackageItems(List<string> uids)
    {
        foreach (string uid in uids)
        {
            DeletePackageItem(uid);
        }
    }

    public void DeletePackageItem(string uid) {
        PackageLocalItem deleteItem = GameManager.Instance.GetPackageLocalItemByUId(uid);
        if (deleteItem == null)
        {
            return;
        }
        PackageLocalData.Instance.items.Remove(deleteItem);
        PackageLocalData.Instance.SavePackage();
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
        UIManager.Instance.OpenPanel("Background");
        UIManager.Instance.OpenPanel("DeadMenu");

        Time.timeScale = 0f;
    }
}

public class PackageItemComparer : IComparer<PackageLocalItem>
{
    public int Compare(PackageLocalItem a, PackageLocalItem b)
    {
        PackageTableItem x = GameManager.Instance.GetPackageTableItemById(a.id);
        PackageTableItem y = GameManager.Instance.GetPackageTableItemById(b.id);

        int starComparison = y.star.CompareTo(x.star);

        if (starComparison == 0)
        {
            int idComparison = y.id.CompareTo(x.id);

            if (idComparison == 0)
            {
                return b.level.CompareTo(a.level);
            }

            return idComparison;
        }

        return starComparison;
    }
}

public class GameConst
{
    public const int InventoryWeaponType = 0;

    public const int InventoryFoodType = 1;
}