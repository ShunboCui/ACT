using TMPro;
using UnityEngine;

public class ButtonScript : MonoBehaviour
{
    public GameObject Text1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ButtonPressed()
    {
        //Debug.Log("Button pressed");
        TextMeshProUGUI textMeshProComponent = Text1.GetComponent<TextMeshProUGUI>();
        textMeshProComponent.text = "Game title";
        Text1.SetActive(true);
    }
}
