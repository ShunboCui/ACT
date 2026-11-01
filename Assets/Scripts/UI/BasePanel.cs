using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    //protected bool isRemove = false;

    protected new string name;
    private bool isClosing;

    // Key this panel was registered with in UIManager.
    public string PanelName
    {
        get { return name; }
    }

    // Layer this panel lives on, assigned by UIManager when it opens the panel.
    public UILayer Layer { get; private set; }

    protected virtual void Awake()
    {

    }

    // Called by UIManager right after the panel is instantiated.
    // args is whatever the caller passed to UIManager.OpenPanel(name, args).
    public void OpenPanel(string name, UILayer layer = UILayer.Normal, object args = null)
    {
        this.name = name;
        this.Layer = layer;
        this.SetActive(true);
        OnOpen(args);
    }

    // Lifecycle hook: runs once when the panel is opened. Read args here
    // instead of reaching into other panels or singletons.
    protected virtual void OnOpen(object args)
    {
    }

    // Lifecycle hook: runs once right before the panel is destroyed.
    // Unsubscribe events / release resources here.
    protected virtual void OnClose()
    {
    }

    protected virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void ClosePanel()
    {
        // Several buttons may call ClosePanel in the same frame, and Destroy
        // is deferred to the end of the frame, so guard against a second call.
        if (isClosing)
        {
            return;
        }
        isClosing = true;

        OnClose();
        this.SetActive(false);

        // Removes the panel from the opened list and raises UIManager.PanelClosed.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HandlePanelClosed(name, this);
        }
        Destroy(gameObject);
    }

}
