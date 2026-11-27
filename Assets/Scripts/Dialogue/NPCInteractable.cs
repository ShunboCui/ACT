using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private QuestDefinition questDefinition;
    private bool playerInRange; 
    private HUDPanel Hud => UIManager.Instance.GetPanel<HUDPanel>(UIConst.HUDPanel);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Hud.BindInteractionButton(dialogueData, () => QuestManager.Instance.AcceptQuest(questDefinition));
            Hud.SetInteractionButtonVisible(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Hud.SetInteractionButtonVisible(false);
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
            DialogueManager.Instance.StartDialogue(dialogueData, () => QuestManager.Instance.AcceptQuest(questDefinition), ()=> QuestManager.Instance.CompleteQuest(questDefinition));
            Hud.SetInteractionButtonVisible(false);
        }
    }
}
