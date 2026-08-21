using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum PackageMode
{
    normal,

    delete,

    sort
}

public class PackagePanel : BasePanel
{
    private Transform UIMenu;

    private Transform UIMenuWeapon;

    private Transform UIMenuFood;

    private Transform UITabName;

    private Transform UICloseBtn;

    private Transform UICenter;

    private Transform UIScrollView;

    private Transform UIDetailPanel;

    private Transform UILeftBtn;

    private Transform UIRightBtn;

    private Transform UIDeletePanel;

    private Transform UIDeleteBackBtn;

    private Transform UIDeleteInfoText;

    private Transform UIDeleteConfirmBtn;

    private Transform UIBottomMenus;

    private Transform UIDeleteBtn;

    private Transform UIDetailBtn;

    public GameObject PackageUIItemPrefab;

    public PackageMode curMode = PackageMode.normal;

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

    private string _selectedUID;

    public string SelectedUID
    {
        get
        {
            return _selectedUID;
        }
        set
        {
            _selectedUID = value;
            RefreshDetail() ;
        }
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
            PackageCell packageCell = cell.GetComponent<PackageCell>();
            //Refresh packageCell
            packageCell.RefreshDeleteState();
        }
    }

    private void RefreshDetail()
    {
        PackageLocalItem chosenItem = GameManager.Instance.GetPackageLocalItemByUId(_chooseUid);
        UIDetailPanel.GetComponent<ItemDetail>().Refresh(chosenItem, this);
    }

    override protected void Awake()
    {
        base.Awake();
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

    private void RefreshUI() 
    {
        RefreshScroll();
    }

    private void RefreshScroll()
    {
        RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
        for (int i = 0; i < scrollContent.childCount; i++)
        {
            Destroy(scrollContent.GetChild(i).gameObject);
        }
        foreach (PackageLocalItem packageLocalItem in PackageLocalData.Instance.LoadPackage())
        {
            Transform PackageUIItem = Instantiate(PackageUIItemPrefab.transform, scrollContent) as Transform;
            PackageCell packageCell = PackageUIItem.GetComponent<PackageCell>();
            packageCell.Refresh(packageLocalItem, this);
        }

    }

    private void InitUIName()
    {
        UIMenu = transform.Find("TopCenter/Menus");
        UIMenuWeapon = transform.Find("TopCenter/Menus/Weapon");
        UIMenuFood = transform.Find("TopCenter/Menus/Food");
        UITabName = transform.Find("TopLeft/TabName");
        UICloseBtn = transform.Find("TopRight/Close");
        UICenter = transform.Find("Center");
        UIScrollView = transform.Find("Center/ScrollView");
        UIDetailPanel = transform.Find("Center/DetailPanel");
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
        UIDetailPanel.gameObject.SetActive(true)    ;
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
        print("<<<<<< onClickDeleteConfirm <<<<<<");
        if (selectedDeleteUid == null)
            return;
        if (selectedDeleteUid.Count == 0) return;
        GameManager.Instance.DeletePackageItems(this.selectedDeleteUid);
        RefreshUI();
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
    }
}
