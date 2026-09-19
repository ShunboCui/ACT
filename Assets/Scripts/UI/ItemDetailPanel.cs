using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : BasePanel
{
    private Transform UIDetailPanel;
    private Transform UICloseButton;
    private PackagePanel packagePanel;
    private PackageTableItem packageTableItem;
    private PackageLocalItem chosenItem;

    private string _chooseUid;

    public string ChooseUID
    {
        get
        {
            return _chooseUid;
        }
        set
        {
            _chooseUid = value;
            RefreshDetail();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitUI();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        packagePanel = UIManager.Instance.GetPanel("PackagePanel") as PackagePanel;
        ChooseUID = packagePanel.ChooseUID;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InitUI()
    {
        InitUIName();
        InitClick();
    }

    private void InitUIName()
    {
        UIDetailPanel = transform.Find("Center/DetailPanel");
        UICloseButton = transform.Find("TopRight/Close");
    }

    private void InitClick()
    {
        UICloseButton.GetComponent<Button>().onClick.AddListener(OnClickClose);
    }

    private void RefreshDetail()
    {
        chosenItem = GameManager.Instance.GetPackageLocalItemByUId(_chooseUid);
        UIDetailPanel.GetComponent<ItemDetail>().Refresh(chosenItem, this);
    }

    private void OnClickClose()
    {
        ClosePanel();
    }

    private void EquipWeapon()
    {
        packageTableItem = GameManager.Instance.GetPackageTableItemById(chosenItem.id);
        WeaponController.Instance.EquipWeapon(packageTableItem);
    }
}
