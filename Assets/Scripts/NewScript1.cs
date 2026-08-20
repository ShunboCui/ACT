using UnityEngine;
using UnityEngine.InputSystem;

public class NewScript1 : MonoBehaviour
{
    public bool myBool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myBool = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) { 
            Debug.Log("Jumping");
        }
    }
}
