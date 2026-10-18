using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : BasePanel
{
    private Transform UIDetailCard;
    private Transform UICloseButton;
    private Transform UIEquipButton;
    private Transform UIDetailsLayer;
    private Transform UIEnhanceLayer;
    private Transform UIEnhanceLevelLabel;
    private Transform UIDetailsSwitchButton;
    private Transform UIEnhanceSwitchButton;
    private Transform UIConfirmEnhanceButton;
    private Transform UIMaterialList;
    private InventoryController controller;

    private InventoryPanel packagePanel;
    private PackageTableItem packageTableItem;
    private InventorySlotItem chosenItem;
    public GameObject MaterialItemPrefab;
    public int totalExpGain;
    public Dictionary<string, int> materialUidCountPair;

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
            RefreshDetailCard();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        controller = new InventoryController();
        InitUI();
        UIDetailsLayer.gameObject.SetActive(true);
        UIEnhanceLayer.gameObject.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        packagePanel = UIManager.Instance.GetPanel("InventoryPanel") as InventoryPanel;
        RefreshUI();
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

    private void RefreshUI()
    {
        ChooseUID = packagePanel.ChooseUID;
        RefreshEnhanceLevel();
    }

    private void InitUIName()
    {
        UIDetailCard = transform.Find("Center/Details/DetailCard");
        UICloseButton = transform.Find("TopRight/Close");
        UIEquipButton = transform.Find("Center/Details/Equip");
        UIDetailsLayer = transform.Find("Center/Details");
        UIEnhanceLayer = transform.Find("Center/Enhance");
        UIMaterialList = transform.Find("Center/Enhance/Materials");
        UIDetailsSwitchButton = transform.Find("Left/Details");
        UIEnhanceSwitchButton = transform.Find("Left/Enhance");
        UIEnhanceLevelLabel = transform.Find("Center/Enhance/Level/Text");
        UIConfirmEnhanceButton = transform.Find("Center/Enhance/Level/Confirm");
    }

    private void InitClick()
    {
        UICloseButton.GetComponent<Button>().onClick.AddListener(OnClickClose);
        UIEquipButton.GetComponent<Button>().onClick.AddListener(EquipWeapon);
        UIDetailsSwitchButton.GetComponent<Button>().onClick.AddListener(SwitchToDetails);
        UIEnhanceSwitchButton.GetComponent<Button>().onClick.AddListener(SwitchToEnhance);
        UIConfirmEnhanceButton.GetComponent<Button>().onClick.AddListener(ConfirmEnhance);
    }

    private void RefreshDetailCard()
    {
        chosenItem = GameManager.Instance.GetInventoryItemByUId(_chooseUid);
        UIDetailCard.GetComponent<ItemDetailCard>().Refresh(chosenItem, this);
    }

    private void OnClickClose()
    {
        ClosePanel();
        packagePanel.RefreshUI();
        packagePanel.ChooseUID = ChooseUID;
    }

    private void EquipWeapon()
    {
        packageTableItem = GameManager.Instance.GetPackageTableItemById(chosenItem.id);
        WeaponController.Instance.EquipWeapon(packageTableItem);
    }

    private void SwitchToDetails()
    {
        UIEnhanceLayer.gameObject.SetActive(false);
        UIDetailsLayer.gameObject.SetActive(true);
    }

    private void SwitchToEnhance()
    {
        UIDetailsLayer.gameObject.SetActive(false);
        UIEnhanceLayer.gameObject.SetActive(true);
        RefreshMaterialSlots();
    }

    private void RefreshMaterialSlots()
    {
        for (int i = 0; i < UIMaterialList.childCount; i++)
        {
            Destroy(UIMaterialList.GetChild(i).gameObject);
        }

        List<InventorySlotItem> inventoryMaterialItems = controller.GetItemsByType(GameConst.InventoryMaterialType);
        if (inventoryMaterialItems.Count == 0) return;

        foreach (InventorySlotItem inventoryMaterialItem in inventoryMaterialItems)
        {
            Transform MaterialUIItem = Instantiate(MaterialItemPrefab.transform, UIMaterialList) as Transform;
            MaterialSlotView materialSlotView = MaterialUIItem.GetComponent<MaterialSlotView>();
            materialSlotView.Refresh(inventoryMaterialItem, this);
        }
    }

    public void AddMaterialUidCount(string uid)
    {
        if (materialUidCountPair == null)
        {
            materialUidCountPair = new Dictionary<string, int>();
        }

        if (materialUidCountPair.Count == 0 || !materialUidCountPair.ContainsKey(uid))
        {
            materialUidCountPair.Add(uid, 1);
        }
        else { materialUidCountPair[uid] += 1; }

        foreach (var item in materialUidCountPair)
        {
            Debug.Log("Materials selected: " + item.Key.ToString() + "count: " + ((int)(item.Value)).ToString());
        }
    }

    private void RefreshEnhanceLevel()
    {
        totalExpGain = 0;
        UIEnhanceLevelLabel.GetComponent<Text>().text = "Level " + chosenItem.level.ToString();
    }

    private void ConfirmEnhance()
    {
        controller.ConfirmWeaponEnhance(chosenItem.uid, totalExpGain);
        controller.ConsumeMaterials(materialUidCountPair);
        if (materialUidCountPair.Count > 0)
        {
            foreach (string uid in materialUidCountPair.Keys.ToList())
            {
                materialUidCountPair[uid] = 0;
            }
        }

        RefreshMaterialSlots();
        RefreshEnhanceLevel();
        InventorySlotData.Instance.SaveInventory();
    }
}
