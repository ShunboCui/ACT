using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    private bool playerInRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            UIManager.Instance.BindInteractionButton(dialogueData);
            UIManager.Instance.EnableInteractionButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            UIManager.Instance.DisableInteractionButton();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerInRange)
            return;

        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.IsOpen)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            DialogueManager.Instance.StartDialogue(dialogueData);
            UIManager.Instance.DisableInteractionButton();
        }
    }
}
