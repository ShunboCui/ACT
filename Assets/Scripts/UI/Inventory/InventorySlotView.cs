using System.Collections.Generic;
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

    // When true, item icons are created once per image path and reused.
    // false keeps the old behaviour (a new Sprite on every Refresh, which is
    // never destroyed). Set by InventoryPanel from its scroll mode.
    public static bool CacheSprites;
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> spriteCache =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    public RectTransform SlotRect { get; private set; }

    private void Awake()
    {
        SlotRect = (RectTransform)transform;
        InitUI();
    }

    private static Sprite GetSprite(string imagePath)
    {
        Sprite sprite;
        if (spriteCache.TryGetValue(imagePath, out sprite) && sprite != null)
        {
            return sprite;
        }

        // Texture imported as Sprite (2D and UI): the sprite already exists.
        sprite = Resources.Load<Sprite>(imagePath);
        if (sprite == null)
        {
            // Fallback: plain texture or a sprite sheet without slices.
            Texture2D t = Resources.Load<Texture2D>(imagePath);
            sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                new Vector2(0, 0), 100f, 0, SpriteMeshType.FullRect);
        }

        spriteCache[imagePath] = sprite;
        return sprite;
    }

    // Called when a recycled slot is hidden, so the next item does not inherit
    // hover / selection visuals.
    public void OnRecycle()
    {
        UIDeleteSelect.gameObject.SetActive(false);
        UISelectAnimation.gameObject.SetActive(false);
        UIMouseOverAnimation.gameObject.SetActive(false);
    }

    // Selection highlight, used by the click handler and when a recycled slot
    // is bound to the item that is currently highlighted.
    public void ShowSelected()
    {
        UISelectAnimation.gameObject.SetActive(true);
        UISelectAnimation.GetComponent<Animator>().SetTrigger("In");
    }

    public string Uid
    {
        get { return inventorySlotItem != null ? inventorySlotItem.uid : null; }
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
        UIIcon.GetComponent<Image>().sprite = GetSprite(this.packageTableItem.imagePath);

        //Refresh stars
        RefreshStars();

        //A recycled slot may carry the delete mark of its previous item
        RefreshDeleteState();
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
        List<string> selected = uiParent.selectedDeleteUid;
        UIDeleteSelect.gameObject.SetActive(selected != null && selected.Contains(this.inventorySlotItem.uid));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UISelectAnimation.gameObject.SetActive(true);
        if (uiParent.curMode == PackageMode.delete)
        {
            uiParent.AddSelectDeleteUid(this.inventorySlotItem.uid);
        }
        UISelectAnimation.GetComponent<Animator>().SetTrigger("In");
        uiParent.HighlightUid = this.inventorySlotItem.uid;
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
