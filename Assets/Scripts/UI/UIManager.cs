using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    private Transform _UIRoot;
    private Dictionary<string, string> pathDict;
    public Dictionary<string, BasePanel> openedPanelDict;
    private Dictionary<string, GameObject> prefabDict;
    private const string prefabPath = "Prefabs/Panels/";

    [SerializeField] Transform InteractionButton;

    public static UIManager Instance
    {
        get {
            return _instance;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitDicts();
    }

    public Transform UIRoot
    {
        get
        {
            if (_UIRoot == null)
            {
                if (GameObject.Find("Canvas"))
                {
                    _UIRoot = GameObject.Find("Canvas").transform;
                }
                else
                {
                    _UIRoot = new GameObject("Canvas").transform;
                }
            }
            return (_UIRoot);
        }
    }

    private void InitDicts()
    {
        prefabDict = new Dictionary<string, GameObject>();
        openedPanelDict = new Dictionary<string, BasePanel>();
        pathDict = new Dictionary<string, string>()
        {
            {UIConst.InventoryPanel, "Inventory/InventoryPanel" },
            {UIConst.WishPanel, "Wish/WishPanel" },
            {UIConst.MainPanel, "MainPanel" },
            {UIConst.VictoryPanel, "VictoryMenu" },
            {UIConst.DeadMenu, "DeadMenu" },
            {UIConst.ItemDetailPanel, "Inventory/ItemDetailPanel" },
            {UIConst.DialoguePanel, "DialoguePanel" }
        };
    }

    public BasePanel GetPanel(string name)
    {
        BasePanel panel = null;
        if (openedPanelDict.TryGetValue(name, out panel))
        {
            return panel;
        }
        return null;
    }

    public BasePanel OpenPanel(string name)
    {
        BasePanel panel = null;
        if (openedPanelDict.TryGetValue(name, out panel))
        {
            Debug.Log("Panel already opened: " +  name);
            return null;
        }

        string path = "";
        if (!pathDict.TryGetValue(name, out path))
        {
            Debug.LogError("Panel name incorrect: " + name);
            return null;
        }

        GameObject panelPrefab = null;
        if (!prefabDict.TryGetValue(name, out panelPrefab))
        {
            string realPath = prefabPath + path;
            panelPrefab = Resources.Load<GameObject>(realPath);
            prefabDict.Add(name, panelPrefab);
        }

        GameObject panelObject = GameObject.Instantiate(panelPrefab, UIRoot, false);
        Debug.Log("Instantiate completed");
        panel = panelObject.GetComponent<BasePanel>();
        panel.OpenPanel(name);
        openedPanelDict.Add(name, panel);
        //

        //Time.timeScale = 0f;
        Debug.Log("panelObject = " + panelObject);
        
        return panel;
    }

    public bool ClosePanel(string name) {
        BasePanel panel = null;
        Debug.Log(openedPanelDict.ToString() + "Length: " + openedPanelDict.Count);
        if (!openedPanelDict.TryGetValue(name, out panel))
        {
            Debug.LogError("Panel not opened: " + name);
            return false;
        }
        //Time.timeScale = 1f;
        panel.ClosePanel();
        return true;
    }

    public void EnableInteractionButton()
    {
        InteractionButton.gameObject.SetActive(true);
    }

    public void DisableInteractionButton()
    {
        InteractionButton.gameObject.SetActive(false);
    }

    public void BindInteractionButton(DialogueData dialogueData, Func<bool> acceptQuest)
    {
        Button button = InteractionButton.gameObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => DialogueManager.Instance.StartDialogue(dialogueData, acceptQuest));
    }
}

public class UIConst
{
    public const string InventoryPanel = "InventoryPanel";
    public const string WishPanel = "WishPanel";
    public const string MainPanel = "MainPanel";
    public const string VictoryPanel = "VictoryMenu";
    public const string DeadMenu = "DeadMenu";
    public const string ItemDetailPanel = "ItemDetailPanel";
    public const string DialoguePanel = "DialoguePanel";

}
