using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PackageCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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

    private PackageLocalItem packageLocalItem;
    private PackageTableItem packageTableItem;
    private PackagePanel uiParent;

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

    public void Refresh(PackageLocalItem packageLocalItem, PackagePanel uiParent)
    {
        this.packageLocalItem = packageLocalItem;
        this.packageTableItem = GameManager.Instance.GetPackageTableItemById(packageLocalItem.id);
        this.uiParent = uiParent;

        //Level info
        UILevel.GetComponent<Text>().text = "Lv. " + this.packageLocalItem.level.ToString();

        //If it is new
        UINew.gameObject.SetActive(this.packageLocalItem.isNew);

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
        if (uiParent.selectedDeleteUid.Contains(this.packageLocalItem.uid))
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
            uiParent.AddSelectDeleteUid(this.packageLocalItem.uid);
        }
        UISelectAnimation.GetComponent<Animator>().SetTrigger("In");
        uiParent.ChooseUID = this.packageLocalItem.uid;
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
