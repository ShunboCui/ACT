using UnityEngine;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    private Transform CloseButton;
    private Transform WishButton;
    private Transform InventoryButton;
    private Transform QuitButton;

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
        CloseButton = transform.Find("TopRight/Close");
        WishButton = transform.Find("TopCenter/Wish");
        InventoryButton = transform.Find("TopCenter/Inventory");
        QuitButton = transform.Find("BottomLeft/Quit");
    }

    private void InitClick()
    {
        CloseButton.gameObject.GetComponent<Button>().onClick.AddListener(OnClickClose);
        WishButton.gameObject.GetComponent<Button>().onClick.AddListener(OnClickWish);
        InventoryButton.gameObject.GetComponent<Button>().onClick.AddListener(OnClickInventory);
    }

    private void OnClickWish()
    {
        UIManager.Instance.OpenPanel(UIConst.WishPanel);
        this.ClosePanel();
    }

    private void OnClickInventory()
    {
        UIManager.Instance.OpenPanel(UIConst.PackagePanel);
        this.ClosePanel();
    }

    private void OnClickClose()
    {
        this.ClosePanel();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
