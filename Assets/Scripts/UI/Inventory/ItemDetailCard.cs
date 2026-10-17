using UnityEngine;
using UnityEngine.UI;

public class ItemDetailCard : MonoBehaviour
{
    private Transform UIStars;

    private Transform UIDescription;

    private Transform UIIcon;

    private Transform UITitle;

    private Transform UILevelText;

    private Transform UISkillDescription;

    private PackageTableItem packageTableItem;

    private InventorySlotItem inventorySlotItem;

    private BasePanel uiParent;

    private void Awake()
    {
        InitUIName();
        //test();
    }
    /*
    private void test()
    {
        Refresh(GameManager.Instance.GetPackageLocalItems()[3], null);
    }*/
    private void InitUIName()
    {
        UIStars = transform.Find("Center/Stars");
        UIDescription = transform.Find("Center/Description");
        UIIcon = transform.Find("Center/Icon");
        UITitle = transform.Find("Top/Title");
        UILevelText = transform.Find("Bottom/LevelPnl/Text");
        UISkillDescription = transform.Find("Bottom/Description");
    }


    public void Refresh(InventorySlotItem inventorySlotItem, BasePanel uiParent)
    {
        this.inventorySlotItem = inventorySlotItem;
        this.packageTableItem = GameManager.Instance.GetPackageTableItemById(inventorySlotItem.id);
        this.uiParent = uiParent;

        //Level info
        UILevelText.GetComponent<Text>().text = "Lv. " + this.inventorySlotItem.level.ToString();

        //Description
        UIDescription.GetComponent<Text>().text = this.packageTableItem.description;

        //Title
        UITitle.GetComponent<Text>().text = this.packageTableItem.name;

        //Skills
        UISkillDescription.GetComponent<Text>().text = this.packageTableItem.skillsDescription;
        
        //Image of the item
        Texture2D t = (Texture2D)Resources.Load(this.packageTableItem.imagePath);
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
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
}
