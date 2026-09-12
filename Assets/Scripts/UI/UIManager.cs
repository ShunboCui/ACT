using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager _instance;
    private Transform _UIRoot;
    private Dictionary<string, string> pathDict;
    public Dictionary<string, BasePanel> panelDict;
    private Dictionary<string, GameObject> prefabDict;

    public static UIManager Instance
    {
        get { 
            if (_instance == null)
            {
                _instance = new UIManager();
            }
            return _instance;
        }
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

    private UIManager()
    {
        InitDicts();
    }

    private void InitDicts()
    {
        prefabDict = new Dictionary<string, GameObject>();
        panelDict = new Dictionary<string, BasePanel>();
        pathDict = new Dictionary<string, string>()
        {
            {UIConst.PackagePanel, "Package/PackagePanel" },
            {UIConst.WishPanel, "Wish/WishPanel" },
            {UIConst.MainPanel, "MainPanel" },
            {UIConst.VictoryPanel, "VictoryMenu" },
            {UIConst.DeadMenu, "DeadMenu" },
            {UIConst.Background, "Background" }
        };
    }

    public BasePanel GetPanel(string name)
    {
        BasePanel panel = null;
        if (panelDict.TryGetValue(name, out panel))
        {
            return panel;
        }
        return null;
    }

    public BasePanel OpenPanel(string name)
    {
        BasePanel panel = null;
        if (panelDict.TryGetValue(name, out panel))
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
            string realPath = "Prefabs/Panels/" + path;
            panelPrefab = Resources.Load<GameObject>(realPath);
            prefabDict.Add(name, panelPrefab);
        }

        GameObject panelObject = GameObject.Instantiate(panelPrefab, UIRoot, false);
        Debug.Log("Instantiate执行完成");
        panel = panelObject.GetComponent<BasePanel>();
        panel.OpenPanel(name);
        panelDict.Add(name, panel);
        //

        //Time.timeScale = 0f;
        Debug.Log("panelObject = " + panelObject);

        panel = panelObject.GetComponent<BasePanel>();
        
        return panel;
    }

    public bool ClosePanel(string name) {
        BasePanel panel = null;
        if (!panelDict.TryGetValue(name, out panel))
        {
            Debug.LogError("Panel not opened: " + name);
            return false;
        }
        //Time.timeScale = 1f;
        panel.ClosePanel();
        return true;
    }
}

public class UIConst
{
    public const string PackagePanel = "PackagePanel";
    public const string WishPanel = "WishPanel";
    public const string MainPanel = "MainPanel";
    public const string VictoryPanel = "VictoryMenu";
    public const string DeadMenu = "DeadMenu";
    public const string Background = "Background";

}
