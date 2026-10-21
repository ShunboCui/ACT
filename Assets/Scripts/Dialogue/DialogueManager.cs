using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public bool IsOpen { get; private set; }


    // Optional callback for quest acceptance.
    private Action onAcceptQuest;

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private float previousTimeScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        // E starts a conversation; Space advances it.
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            NextLine();
    }

    public void StartDialogue(
        DialogueData data,
        Action acceptQuestCallback = null)
    {
        if (IsOpen || data == null ||
            data.lines == null || data.lines.Count == 0)
            return;

        currentDialogue = data;
        currentLineIndex = 0;
        onAcceptQuest = acceptQuestCallback;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsOpen = true;

        // Open DialoguePanel through your existing UIRoot /
        // panel manager, then display the first line.
        OpenDialoguePanel();
        RefreshDialoguePanel();
    }

    public void NextLine()
    {
        if (!IsOpen)
            return;

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            CloseDialogue();
            return;
        }

        RefreshDialoguePanel();
    }

    public void AcceptQuest()
    {
        if (!IsOpen || onAcceptQuest == null)
            return;

        onAcceptQuest.Invoke();
        CloseDialogue();
    }


    public void CloseDialogue()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        // Close DialoguePanel through your existing panel manager.
        CloseDialoguePanel();

        Time.timeScale = previousTimeScale;

        currentDialogue = null;
        onAcceptQuest = null;
        currentLineIndex = 0;
    }

    private void OpenDialoguePanel()
    {
        // Integration point: open the DialoguePanel through
        // your existing panel manager and obtain its component.
    }

    private void RefreshDialoguePanel()
    {
        // Integration point: tell DialoguePanel to display:
        // currentDialogue.speakerName
        // currentDialogue.lines[currentLineIndex]
        // whether this is the final line
    }

    private void CloseDialoguePanel()
    {
        // Integration point: close the panel through your
        // existing panel manager.
    }

}
