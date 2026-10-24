using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    //protected bool isRemove = false;

    protected new string name;

    protected virtual void Awake()
    {

    }

    public void OpenPanel(string name)
    {
        this.name = name;
        this.SetActive(true);
    }

    protected virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void ClosePanel()
    {
        //isRemove = true;
        this.SetActive(false);
        Destroy(gameObject);
        if (UIManager.Instance.openedPanelDict.ContainsKey(name)) {
            UIManager.Instance.openedPanelDict.Remove(name);
        }
    }

}
