using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.Profiling;

public enum PackageMode
{
    normal,

    delete,

    sort
}

// How the item grid is filled. Switch it on the InventoryPanel prefab to
// compare the three in the Profiler (see Docs/InventoryScrollBenchmark.md).
public enum InventoryScrollMode
{
    Original, // destroy every slot and instantiate new ones on each refresh
    Pooled,   // keep the slots, rebind them, hide the extra ones
    Virtual   // only slots inside the viewport exist (InventoryVirtualGrid)
}

public class InventoryPanel : BasePanel
{
    private Transform UIMenu;

    private Transform UIMenuWeapon;

    private Transform UIMenuFood;

    private Transform UIMenuWeaponIcon1;

    private Transform UIMenuWeaponIcon2;

    private Transform UIMenuWeaponIconSelected;

    private Transform UIMenuFoodIcon1;

    private Transform UIMenuFoodIcon2;

    private Transform UIMenuFoodIconSelected;

    private Transform UIWeaponCount;

    private Transform UITabName;

    private Transform UICloseBtn;

    private Transform UICenter;

    private Transform UIScrollView;

    private Transform UIDetailCard;

    private Transform UILeftBtn;

    private Transform UIRightBtn;

    private Transform UIDeletePanel;

    private Transform UIDeleteBackBtn;

    private Transform UIDeleteInfoText;

    private Transform UIDeleteConfirmBtn;

    private Transform UIBottomMenus;

    private Transform UIDeleteBtn;

    private Transform UIDetailBtn;
    private InventoryController controller;

    public GameObject InventorySlotUIPrefab;

    public PackageMode curMode = PackageMode.normal;
    private const int maxWeaponCount = 1000;

    private string _chooseUid;

    public string ChooseUID
    {
        get {
            return _chooseUid;
        }
        set {
            _chooseUid = value;
            RefreshDetail();
        }
    }

    public List<string> selectedDeleteUid;

    [SerializeField] private InventoryScrollMode scrollMode = InventoryScrollMode.Original;
    [SerializeField] private bool logBenchmark;

    private static readonly ProfilerMarker refreshScrollMarker = new ProfilerMarker("Inventory.RefreshScroll");

    private ScrollRect scrollRect;
    private InventoryVirtualGrid virtualGrid;
    private readonly List<InventorySlotView> slotPool = new List<InventorySlotView>();
    private List<InventorySlotItem> currentItems = new List<InventorySlotItem>();

    // The slot the player clicked last. Recycled slots re-show the highlight
    // when they are bound to this item again.
    public string HighlightUid { get; set; }

    override protected void Awake()
    {
        base.Awake();
        controller = new InventoryController();
        InitUI();
        SetupScrollMode();
    }

    private void SetupScrollMode()
    {
        scrollRect = UIScrollView.GetComponent<ScrollRect>();
        InventorySlotView.CacheSprites = scrollMode != InventoryScrollMode.Original;

        if (scrollMode == InventoryScrollMode.Virtual)
        {
            // Read the layout numbers before the grid layout gets disabled.
            GridLayoutGroup grid = scrollRect.content.GetComponent<GridLayoutGroup>();
            virtualGrid = UIScrollView.gameObject.AddComponent<InventoryVirtualGrid>();
            virtualGrid.Setup(InventorySlotUIPrefab.GetComponent<InventorySlotView>(), BindSlot,
                grid.cellSize, grid.spacing, grid.padding);
        }
    }

    private void BindSlot(int index, InventorySlotView view)
    {
        view.Refresh(currentItems[index], this);
        if (view.Uid == HighlightUid)
        {
            view.ShowSelected();
        }
    }

    public void Start()
    {
        RefreshUI();
    }

    private void InitUI()
    {
        InitUIName();
        InitClick();
    }

    public void RefreshUI()
    {
        onClickWeapon();
        RefreshDetail();
        RefreshCount();
    }

    public void AddSelectDeleteUid(string uid)
    {
        if (selectedDeleteUid == null)
        {
            selectedDeleteUid = new List<string>();
        }
        if (!selectedDeleteUid.Contains(uid))
        {
            selectedDeleteUid.Add(uid);
        }
        else
        {
            selectedDeleteUid.Remove(uid);
        }

        RefreshDeletePanel();
    }

    private void RefreshDeletePanel()
    {
        switch (scrollMode)
        {
            case InventoryScrollMode.Pooled:
                for (int i = 0; i < currentItems.Count; i++)
                {
                    slotPool[i].RefreshDeleteState();
                }
                break;

            case InventoryScrollMode.Virtual:
                foreach (InventorySlotView view in virtualGrid.ActiveSlots)
                {
                    view.RefreshDeleteState();
                }
                break;

            default:
                RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
                foreach (Transform cell in scrollContent)
                {
                    InventorySlotView packageCell = cell.GetComponent<InventorySlotView>();
                    //Refresh packageCell
                    packageCell.RefreshDeleteState();
                }
                break;
        }
    }

    private void RefreshScroll(int itemType)
    {
        int gcBefore = System.GC.CollectionCount(0);
        long start = System.Diagnostics.Stopwatch.GetTimestamp();

        using (refreshScrollMarker.Auto())
        {
            switch (scrollMode)
            {
                case InventoryScrollMode.Pooled:
                    RefreshScrollPooled(itemType);
                    break;
                case InventoryScrollMode.Virtual:
                    RefreshScrollVirtual(itemType);
                    break;
                default:
                    RefreshScrollOriginal(itemType);
                    break;
            }
        }

        if (logBenchmark)
        {
            // Only the synchronous part: Destroy and the layout / canvas
            // rebuild happen later in the frame. Use the Profiler for the full cost.
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Debug.Log("[Inventory] " + scrollMode + " type=" + itemType + " " + ms.ToString("F2")
                + " ms (call only), gen0 GCs during call: " + (System.GC.CollectionCount(0) - gcBefore));
        }
    }

    // Selection state shared by the Pooled and Virtual modes.
    private void ResetSelection()
    {
        HighlightUid = null;
        if (currentItems.Count > 0)
        {
            UIDetailCard.gameObject.SetActive(true);
            ChooseUID = currentItems[0].uid;
        }
        else
        {
            // Nothing to show: do not leave the previous tab's item on the card.
            _chooseUid = null;
            UIDetailCard.gameObject.SetActive(false);
        }
    }

    private void RefreshScrollPooled(int itemType)
    {
        currentItems = controller.GetItemsByType(itemType);
        ResetSelection();

        RectTransform content = scrollRect.content;
        for (int i = 0; i < currentItems.Count; i++)
        {
            if (i == slotPool.Count)
            {
                slotPool.Add(Instantiate(InventorySlotUIPrefab, content).GetComponent<InventorySlotView>());
            }
            InventorySlotView view = slotPool[i];
            if (!view.gameObject.activeSelf)
            {
                view.gameObject.SetActive(true);
            }
            BindSlot(i, view);
        }

        // Hide the surplus, keep them for the next refresh.
        for (int i = currentItems.Count; i < slotPool.Count; i++)
        {
            InventorySlotView view = slotPool[i];
            if (view.gameObject.activeSelf)
            {
                view.OnRecycle();
                view.gameObject.SetActive(false);
            }
        }

        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshScrollVirtual(int itemType)
    {
        currentItems = controller.GetItemsByType(itemType);
        ResetSelection();
        virtualGrid.SetItemCount(currentItems.Count, false);
    }

    private void RefreshScrollOriginal(int itemType)
    {
        RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
        for (int i = 0; i < scrollContent.childCount; i++)
        {
            Destroy(scrollContent.GetChild(i).gameObject);
        }

        //List<PackageLocalItem> packageItems = PackageLocalData.Instance.LoadPackage();
        //List<PackageLocalItem> packageItems = GameManager.Instance.GetSortedPackageLocalItems();
        //List<PackageLocalItem> packageWeaponItems = GameManager.Instance.GetPackageLocalItemsByType(GameConst.InventoryWeaponType);
        List<InventorySlotItem> inventoryWeaponItems = controller.GetItemsByType(itemType);
        if (inventoryWeaponItems.Count == 0) return;

        ChooseUID = inventoryWeaponItems[0].uid;
        foreach (InventorySlotItem inventorySlotItem in inventoryWeaponItems)
        {
            Transform PackageUIItem = Instantiate(InventorySlotUIPrefab.transform, scrollContent) as Transform;
            InventorySlotView inventorySlot = PackageUIItem.GetComponent<InventorySlotView>();
            inventorySlot.Refresh(inventorySlotItem, this);
        }
    }

    private void RefreshDetail()
    {
        if (_chooseUid == null) return;
        InventorySlotItem chosenItem = GameManager.Instance.GetInventoryItemByUId(_chooseUid);
        UIDetailCard.GetComponent<ItemDetailCard>().Refresh(chosenItem, this);
    }

    private void RefreshCount()
    {
        UIWeaponCount.gameObject.GetComponent<Text>().text = "Weapon " + GameManager.Instance.GetInventorySlotItemsByType(GameConst.InventoryWeaponType).Count().ToString() + "/" + maxWeaponCount.ToString();
    }

    private void InitUIName()
    {
        UIMenu = transform.Find("TopCenter/Menus");
        UIMenuWeapon = transform.Find("TopCenter/Menus/Weapon");
        UIMenuFood = transform.Find("TopCenter/Menus/Food");
        UIMenuWeaponIcon1 = transform.Find("TopCenter/Menus/Weapon/Icon1");
        UIMenuWeaponIcon2 = transform.Find("TopCenter/Menus/Weapon/Icon2");
        UIMenuFoodIcon1 = transform.Find("TopCenter/Menus/Food/Icon1");
        UIMenuFoodIcon2 = transform.Find("TopCenter/Menus/Food/Icon2");

        UIWeaponCount = transform.Find("TopRight/NumText");
        UITabName = transform.Find("TopLeft/TabName");
        UICloseBtn = transform.Find("TopRight/Close");
        UICenter = transform.Find("Center");
        UIScrollView = transform.Find("Center/ScrollView");
        UIDetailCard = transform.Find("Center/DetailCard");
        UILeftBtn = transform.Find("Left/Button");
        UIRightBtn = transform.Find("Right/Button");
        UIDeletePanel = transform.Find("Bottom/DeletePanel");
        UIDeleteBackBtn = transform.Find("Bottom/DeletePanel/Back");
        UIDeleteInfoText = transform.Find("Bottom/DeletePanel/InfoText");
        UIDeleteConfirmBtn = transform.Find("Bottom/DeletePanel/ConfirmButton");
        UIBottomMenus = transform.Find("Bottom/BottomMenus");
        UIDeleteBtn = transform.Find("Bottom/BottomMenus/DeleteButton");
        UIDetailBtn = transform.Find("Bottom/BottomMenus/DetailButton");

        UIDeletePanel.gameObject.SetActive(false);
        UIDetailCard.gameObject.SetActive(true)    ;
    }

    private void InitClick()
    {
        UIMenuWeapon.GetComponent<Button>().onClick.AddListener(onClickWeapon);
        UIMenuFood.GetComponent<Button>().onClick.AddListener(onClickFood);
        UICloseBtn.GetComponent<Button>().onClick.AddListener(onClickClose);
        UILeftBtn.GetComponent<Button>().onClick.AddListener(onClickLeft);
        UIRightBtn.GetComponent<Button>().onClick.AddListener(onClickRight);

        UIDeleteBackBtn.GetComponent<Button>().onClick.AddListener(onClickDeleteBack);
        UIDeleteConfirmBtn.GetComponent<Button>().onClick.AddListener(onClickDeleteConfirm);
        UIDeleteBtn.GetComponent<Button>().onClick.AddListener(onClickDelete);
        UIDetailBtn.GetComponent<Button>().onClick.AddListener(onClickDetail);
    }

    private void onClickWeapon()
    {
        RefreshScroll(GameConst.InventoryWeaponType);
        SetTab(true);
    }

    private void onClickFood()
    {
        RefreshScroll(GameConst.InventoryFoodType);
        SetTab(false);
    }

    // Esc / back: leave delete mode first, otherwise same as the close button.
    protected override void OnBack()
    {
        if (curMode == PackageMode.delete)
        {
            onClickDeleteBack();
        }
        else
        {
            onClickClose();
        }
    }

    private void onClickClose()
    {
        print("<<<<<< onClickClose <<<<<<");
        //UIManager.Instance.ClosePanel(UIConst.PackagePanel);
        ClosePanel();
        UIManager.Instance.OpenPanel(UIConst.MainPanel);
    }

    private void onClickLeft()
    {
        print("<<<<<< onClickLeft <<<<<<");
    }

    private void onClickRight()
    {
        print("<<<<<< onClickRight <<<<<<");
    }

    private void onClickDeleteBack()
    {
        print("<<<<<< onClickDeleteBack <<<<<<");
        curMode = PackageMode.normal;
        UIDeletePanel.gameObject.SetActive(false);
        selectedDeleteUid = new List<string>();
        RefreshDeletePanel();
    }

    private void onClickDeleteConfirm()
    {
        if (controller.DeleteItems(this.selectedDeleteUid))
        {
            RefreshUI();
        }
    }

    private void onClickDelete()
    {
        print("<<<<<< onClickDelete <<<<<<");
        curMode = PackageMode.delete;
        UIDeletePanel.gameObject.SetActive(true);
    }

    private void onClickDetail()
    {
        print("<<<<<< onClickDetail <<<<<<");
        UIManager.Instance.OpenPanel("ItemDetailPanel");
    }

#if UNITY_EDITOR
    // Benchmark helpers: Inspector -> InventoryPanel -> three-dot menu (Play mode).
    // The test items are NOT saved, but any later SaveInventory() (pull, delete,
    // enhance) would write them to PlayerPrefs, so remove them when done.
    [ContextMenu("Bench/Add 500 test weapons")]
    private void BenchAddItems()
    {
        List<InventorySlotItem> items = GameManager.Instance.GetInventorySaveData();
        List<PackageTableItem> weapons = GameManager.Instance.GetPackageTableByType(GameConst.InventoryWeaponType);
        for (int i = 0; i < 500; i++)
        {
            items.Add(new InventorySlotItem()
            {
                uid = "bench-" + System.Guid.NewGuid(),
                id = weapons[Random.Range(0, weapons.Count)].id,
                num = 1,
                exp = 0,
                isNew = false
            });
        }
        RefreshUI();
    }

    [ContextMenu("Bench/Remove test weapons")]
    private void BenchRemoveItems()
    {
        GameManager.Instance.GetInventorySaveData().RemoveAll(x => x.uid.StartsWith("bench-"));
        RefreshUI();
    }
#endif

    private void SetTab(bool weaponActive)
    {
        UIMenuWeaponIcon1.gameObject.SetActive(!weaponActive);
        UIMenuWeaponIcon2.gameObject.SetActive(weaponActive);
        UIMenuFoodIcon1.gameObject.SetActive(weaponActive);
        UIMenuFoodIcon2.gameObject.SetActive(!weaponActive);
    }
}
