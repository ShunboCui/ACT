using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialSlotView : MonoBehaviour, IPointerClickHandler
{
    private Transform UIIcon;
    private Transform UICount;
    private ItemDetailPanel uiParent;
    private PackageTableItem packageTableItem;
    private InventorySlotItem inventoryMaterialItem;
    private int selectedCount;


    private void Awake()
    {
        InitUI();
        selectedCount = 0;
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
    }

    private void InitUIName()
    {
        UICount = transform.Find("Top/SelectedNumber");
        UIIcon = transform.Find("Center/Icon");
        Debug.Log(UICount);
        Debug.Log(UIIcon);
    }

    public void Refresh(InventorySlotItem inventoryMaterialItem, ItemDetailPanel uiParent)
    {
        this.inventoryMaterialItem = inventoryMaterialItem;
        this.packageTableItem = GameManager.Instance.GetPackageTableItemById(inventoryMaterialItem.id);
        this.uiParent = uiParent;

        //Image of the item
        Texture2D t = (Texture2D)Resources.Load(this.packageTableItem.imagePath);
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
        UIIcon.GetComponent<Image>().sprite = temp;
        RefreshNumber();
    }

    private void RefreshNumber()
    {
        UICount.GetComponent<Text>().text = selectedCount.ToString() + "/" + inventoryMaterialItem.num.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Mouse click");
        selectedCount += 1;
        uiParent.selectedExp += 100;
        RefreshNumber();
    }
}
