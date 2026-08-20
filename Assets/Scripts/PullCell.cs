using UnityEngine;
using UnityEngine.UI;

public class PullCell : MonoBehaviour
{
    private Transform UIImage;
    private Transform UIStars;
    private PackageLocalItem packageLocalItem;
    private PackageTableItem packageTableItem;
    private WishPanel uiParent;

    private void Awake()
    {
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
    }

    private void InitUIName()
    {
        UIImage = transform.Find("Center/Image");
        UIStars = transform.Find("Bottom/Stars");
    }

    public void Refresh(PackageLocalItem packageLocalItem, WishPanel uiParent)
    {
        this.packageLocalItem = packageLocalItem;
        this.packageTableItem = GameManager.Instance.GetPackageTableItemById(packageLocalItem.id);
        this.uiParent = uiParent;


        //Image of the item
        RefreshImage();

        //Refresh stars
        RefreshStars();
    }

    private void RefreshImage()
    {

        Texture2D t = (Texture2D)Resources.Load(this.packageTableItem.imagePath);
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
        UIImage.GetComponent<Image>().sprite = temp;
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
