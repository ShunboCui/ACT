using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : BasePanel
{
    private Transform AcceptQuestButton;
    private Transform CompleteQuestButton;
    private Transform CloseButton;
    private Transform CharacterName;
    private Transform DialogueLine;
    private Transform NextLine;


    override protected void Awake()
    {
        base.Awake();
        InitUI();
        DialogueManager.Instance.currentDialoguePanel = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DisableButtons();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InitUI()
    {
        InitUIName();
        InitClick();
    }

    private void InitUIName()
    {
        AcceptQuestButton = transform.Find("BottomRight/AcceptQuestButton");
        CompleteQuestButton = transform.Find("BottomRight/CompleteQuestButton");
        CloseButton = transform.Find("BottomRight/CloseButton");
        CharacterName = transform.Find("TopLeft/CharacterNameText");
        DialogueLine = transform.Find("Center/DialogueLine");
        NextLine = transform.Find("Center/NextLine");
    }

    private void InitClick()
    {
        AcceptQuestButton.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.AcceptQuest);
        CompleteQuestButton.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.CompleteQuest);
        CloseButton.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.CloseDialogue);
        NextLine.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.NextLine);
    }

    public void RefreshDialogue(string speakerName, string currentLine)
    {
        CharacterName.GetComponent<TextMeshProUGUI>().text = speakerName;
        DialogueLine.GetComponent<TextMeshProUGUI>().text = currentLine;
    }

    public void EnableButtons()
    {
        List<GameObject> buttons = new List<GameObject>() {
            AcceptQuestButton.gameObject, CompleteQuestButton.gameObject, CloseButton.gameObject
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].SetActive(true);
            }
        }
    }

    public void DisableButtons()
    {
        List<GameObject> buttons = new List<GameObject>() {
            AcceptQuestButton.gameObject, CompleteQuestButton.gameObject, CloseButton.gameObject
        };

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].SetActive(false);
            }
        }
    }
}
