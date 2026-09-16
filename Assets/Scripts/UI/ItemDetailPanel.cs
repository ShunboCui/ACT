using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : BasePanel
{
    private Transform UIDetailPanel;
    private Transform UICloseButton;
    private PackagePanel packagePanel;

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
        UICloseButton.GetComponent<Button>().onClick.AddListener(onClickClose);
    }

    private void RefreshDetail()
    {
        PackageLocalItem chosenItem = GameManager.Instance.GetPackageLocalItemByUId(_chooseUid);
        UIDetailPanel.GetComponent<ItemDetail>().Refresh(chosenItem, this);
    }

    private void onClickClose()
    {
        ClosePanel();
    }
}
