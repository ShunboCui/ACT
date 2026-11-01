using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draw order of panels. A panel on a higher layer always renders above every
// panel on a lower layer, no matter which one was opened first.
public enum UILayer
{
    Background = 0, // full screen backdrops
    Normal = 1,     // regular menus: main menu, inventory, wish, dialogue
    Popup = 2,      // dialogs opened on top of a menu: item detail
    Top = 3         // always on top: death / victory screens
}

// Registration data of one panel. Add a new panel by registering it in
// UIManager.RegisterPanels, no other UIManager code needs to change.
public class UIPanelInfo
{
    public string name;
    public string path;     // relative to Resources/Prefabs/Panels/
    public UILayer layer;
}

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    private Transform _UIRoot;
    private readonly Dictionary<string, UIPanelInfo> panelInfoDict = new Dictionary<string, UIPanelInfo>();
    private readonly Dictionary<string, BasePanel> openedPanelDict = new Dictionary<string, BasePanel>();
    private readonly Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    private readonly Dictionary<UILayer, Transform> layerRootDict = new Dictionary<UILayer, Transform>();
    private const string prefabPath = "Prefabs/Panels/";

    // Raised after a panel is opened / closed, with the panel name.
    public event Action<string> PanelOpened;
    public event Action<string> PanelClosed;

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
        RegisterPanels();
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

    private void RegisterPanels()
    {
        Register(UIConst.MainPanel, "MainPanel", UILayer.Normal);
        Register(UIConst.InventoryPanel, "Inventory/InventoryPanel", UILayer.Normal);
        Register(UIConst.WishPanel, "Wish/WishPanel", UILayer.Normal);
        Register(UIConst.DialoguePanel, "DialoguePanel", UILayer.Normal);
        Register(UIConst.ItemDetailPanel, "Inventory/ItemDetailPanel", UILayer.Popup);
        Register(UIConst.VictoryPanel, "VictoryMenu", UILayer.Top);
        Register(UIConst.DeadMenu, "DeadMenu", UILayer.Top);
    }

    public void Register(string name, string path, UILayer layer = UILayer.Normal)
    {
        panelInfoDict[name] = new UIPanelInfo()
        {
            name = name,
            path = path,
            layer = layer
        };
    }

    // ---------- Layers ----------

    private Transform GetLayerRoot(UILayer layer)
    {
        Transform root;
        if (!layerRootDict.TryGetValue(layer, out root) || root == null)
        {
            EnsureLayerRoots();
            root = layerRootDict[layer];
        }
        return root;
    }

    // Creates every missing layer root and puts all of them at the end of the
    // canvas children in enum order, so sibling order == layer order.
    private void EnsureLayerRoots()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            Transform root;
            if (!layerRootDict.TryGetValue(layer, out root) || root == null)
            {
                layerRootDict[layer] = CreateLayerRoot(layer);
            }
        }

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            layerRootDict[layer].SetAsLastSibling();
        }
    }

    private Transform CreateLayerRoot(UILayer layer)
    {
        GameObject layerObject = new GameObject("Layer_" + layer, typeof(RectTransform));
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        rect.SetParent(UIRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    // ---------- Query ----------

    public BasePanel GetPanel(string name)
    {
        BasePanel panel = null;
        if (openedPanelDict.TryGetValue(name, out panel))
        {
            return panel;
        }
        return null;
    }

    public T GetPanel<T>(string name) where T : BasePanel
    {
        return GetPanel(name) as T;
    }

    public bool IsOpened(string name)
    {
        return openedPanelDict.ContainsKey(name);
    }

    // ---------- Open / Close ----------

    // args is handed to the panel's OnOpen(args).
    // If the panel is already open, the open panel is returned unchanged.
    public BasePanel OpenPanel(string name, object args = null)
    {
        BasePanel panel = null;
        if (openedPanelDict.TryGetValue(name, out panel))
        {
            Debug.Log("Panel already opened: " +  name);
            return panel;
        }

        UIPanelInfo info = null;
        if (!panelInfoDict.TryGetValue(name, out info))
        {
            Debug.LogError("Panel name incorrect: " + name);
            return null;
        }

        GameObject panelPrefab = LoadPrefab(info);
        if (panelPrefab == null)
        {
            Debug.LogError("Panel prefab not found: " + prefabPath + info.path);
            return null;
        }

        GameObject panelObject = GameObject.Instantiate(panelPrefab, GetLayerRoot(info.layer), false);
        panel = panelObject.GetComponent<BasePanel>();
        if (panel == null)
        {
            Debug.LogError("Panel prefab has no BasePanel component: " + name);
            Destroy(panelObject);
            return null;
        }

        openedPanelDict.Add(name, panel);
        panel.OpenPanel(name, info.layer, args);

        PanelOpened?.Invoke(name);
        return panel;
    }

    public T OpenPanel<T>(string name, object args = null) where T : BasePanel
    {
        return OpenPanel(name, args) as T;
    }

    public bool ClosePanel(string name) {
        BasePanel panel = null;
        if (!openedPanelDict.TryGetValue(name, out panel))
        {
            Debug.LogError("Panel not opened: " + name);
            return false;
        }
        panel.ClosePanel();
        return true;
    }

    public void CloseAllPanels()
    {
        // ClosePanel modifies openedPanelDict, so iterate over a copy.
        List<BasePanel> panels = new List<BasePanel>(openedPanelDict.Values);
        foreach (BasePanel panel in panels)
        {
            if (panel != null)
            {
                panel.ClosePanel();
            }
        }
    }

    // Called by BasePanel.ClosePanel. Only removes the entry if it still
    // belongs to this panel, so a late call for an old panel cannot remove a
    // new panel that was opened with the same name in the meantime.
    internal void HandlePanelClosed(string name, BasePanel panel)
    {
        BasePanel current = null;
        if (openedPanelDict.TryGetValue(name, out current) && current == panel)
        {
            openedPanelDict.Remove(name);
        }
        PanelClosed?.Invoke(name);
    }

    private GameObject LoadPrefab(UIPanelInfo info)
    {
        GameObject panelPrefab = null;
        if (!prefabDict.TryGetValue(info.name, out panelPrefab) || panelPrefab == null)
        {
            panelPrefab = Resources.Load<GameObject>(prefabPath + info.path);
            if (panelPrefab != null)
            {
                prefabDict[info.name] = panelPrefab;
            }
        }
        return panelPrefab;
    }

    // ---------- Interaction button (NPC prompt) ----------

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
