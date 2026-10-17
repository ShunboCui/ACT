using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WishPanel : BasePanel
{
    private Transform CloseButton;
    private Transform SinglePullButton;
    private Transform TenPullsButton;
    private Transform UICenter;
    public GameObject PullItemPrefab;

    override protected void Awake()
    {
        base.Awake();
        InitUI();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
        UICenter = transform.Find("Center");
        CloseButton = transform.Find("TopRight/Close");
        SinglePullButton = transform.Find("Bottom/1Pull");
        TenPullsButton = transform.Find("Bottom/10Pulls");
    }

    private void InitClick()
    {
        CloseButton.GetComponent<Button>().onClick.AddListener(OnClickClose);
        SinglePullButton.GetComponent<Button>().onClick.AddListener(OnClickSinglePull);
        TenPullsButton.GetComponent<Button>().onClick.AddListener(OnClickTenPull);
    }

    private void OnClickClose()
    {
        this.ClosePanel();
        UIManager.Instance.OpenPanel(UIConst.MainPanel);
    }

    private void OnClickSinglePull()
    {
        for (int i = 0; i < UICenter.childCount; i++)
        {
            Destroy(UICenter.GetChild(i).gameObject);
        }

        InventorySlotItem singlePullItem = GameManager.Instance.PullRandom();

        Transform wishCellItem = Instantiate(PullItemPrefab.transform, UICenter) as Transform;

        PullCell pullCell = wishCellItem.GetComponent<PullCell>();
        pullCell.Refresh(singlePullItem, this);

    }
    private void OnClickTenPull()
    {
        for (int i = 0; i < UICenter.childCount; i++)
        {
            Destroy(UICenter.GetChild(i).gameObject);
        }

        List<InventorySlotItem> tenPullsItems = GameManager.Instance.PullRandom10();

        for (int i = 0; i < tenPullsItems.Count; i++)
        {
            Transform wishCellItem = Instantiate(PullItemPrefab.transform, UICenter) as Transform;
            PullCell pullCell = wishCellItem.GetComponent<PullCell>();
            pullCell.Refresh(tenPullsItems[i], this);
        }
    }
}
