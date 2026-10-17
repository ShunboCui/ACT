using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum PackageMode
{
    normal,

    delete,

    sort
}

public class InventoryPanel : BasePanel
{
    private Transform UIMenu;

    private Transform UIMenuWeapon;

    private Transform UIMenuFood;

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

    override protected void Awake()
    {
        base.Awake();
        controller = new InventoryController();
        InitUI();
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
        RefreshScroll();
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
        RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
        foreach (Transform cell in scrollContent)
        {
            InventorySlotView packageCell = cell.GetComponent<InventorySlotView>();
            //Refresh packageCell
            packageCell.RefreshDeleteState();
        }
    }

    private void RefreshScroll()
    {
        RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
        for (int i = 0; i < scrollContent.childCount; i++)
        {
            Destroy(scrollContent.GetChild(i).gameObject);
        }

        //List<PackageLocalItem> packageItems = PackageLocalData.Instance.LoadPackage();
        //List<PackageLocalItem> packageItems = GameManager.Instance.GetSortedPackageLocalItems();
        //List<PackageLocalItem> packageWeaponItems = GameManager.Instance.GetPackageLocalItemsByType(GameConst.InventoryWeaponType);
        List<InventorySlotItem> inventoryWeaponItems = controller.GetItemsByType(GameConst.InventoryWeaponType);
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
        print("<<<<<< onClickWeapon <<<<<<");
    }

    private void onClickFood()
    {
        print("<<<<<< onClickFood <<<<<<");
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
}
