using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Transform UIIcon;
    private Transform UICharacter;
    private Transform UINew;
    private Transform UISelect;
    private Transform UILevel;
    private Transform UIStars;
    private Transform UIDeleteSelect;

    private Transform UISelectAnimation;
    private Transform UIMouseOverAnimation;

    private InventorySlotItem inventorySlotItem;
    private PackageTableItem packageTableItem;
    private InventoryPanel uiParent;

    private void Awake()
    {
        InitUI();
    }

    private void InitUI()
    {
        InitUIName();
    }

    // Update is called once per frame
    private void InitUIName()
    {
        UIIcon = transform.Find("Top/Icon");
        UICharacter = transform.Find("Top/Character");
        UINew = transform.Find("Top/New");
        UISelect = transform.Find("Select");
        UILevel = transform.Find("Bottom/LevelText");
        UIStars = transform.Find("Bottom/Stars");
        UIDeleteSelect = transform.Find("DeleteSelect");
        UISelectAnimation = transform.Find("SelectionAnimation");
        UIMouseOverAnimation = transform.Find("MouseOverAnimation");

        UIDeleteSelect.gameObject.SetActive(false);
        UISelectAnimation.gameObject.SetActive(false);
        //UIMouseOverAnimation.gameObject.SetActive(false);
    }

    public void Refresh(InventorySlotItem inventorySlotItem, InventoryPanel uiParent)
    {
        this.inventorySlotItem = inventorySlotItem;
        this.packageTableItem = GameManager.Instance.GetPackageTableItemById(inventorySlotItem.id);
        this.uiParent = uiParent;

        //Level info
        UILevel.GetComponent<Text>().text = "Lv. " + this.inventorySlotItem.level.ToString();

        //If it is new
        UINew.gameObject.SetActive(this.inventorySlotItem.isNew);

        //Image of the item
        Texture2D t = (Texture2D)Resources.Load(this.packageTableItem.imagePath);
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0,0));
        UIIcon.GetComponent<Image>().sprite = temp;

        //Refresh stars
        RefreshStars();
    }

    public void RefreshStars()
    {
        int starNum = this.packageTableItem.star;
        for (int i = 0; i < UIStars.childCount; i++)
        {
            if (i + 1 <= starNum)
            {
                UIStars.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                UIStars.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public void RefreshDeleteState()
    {
        if (uiParent.selectedDeleteUid.Contains(this.inventorySlotItem.uid))
        {
            UIDeleteSelect.gameObject.SetActive(true);
        }
        else
        {
            UIDeleteSelect.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UISelectAnimation.gameObject.SetActive(true);
        if (uiParent.curMode == PackageMode.delete)
        {
            uiParent.AddSelectDeleteUid(this.inventorySlotItem.uid);
        }
        UISelectAnimation.GetComponent<Animator>().SetTrigger("In");
        uiParent.ChooseUID = this.inventorySlotItem.uid;
        Debug.Log("Mouse click");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIMouseOverAnimation.gameObject.SetActive(true);
        UIMouseOverAnimation.GetComponent<Animator>().SetTrigger("In");
        Debug.Log("Mouse entering");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIMouseOverAnimation.GetComponent<Animator>().SetTrigger("Out");
        Debug.Log("Mouse exiting");
    }
}
