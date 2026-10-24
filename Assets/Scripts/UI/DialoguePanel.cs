using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : BasePanel
{
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
        CloseButton = transform.Find("BottomRight/CloseButton");
        CharacterName = transform.Find("TopLeft/CharacterNameText");
        DialogueLine = transform.Find("Center/DialogueLine");
        NextLine = transform.Find("Center/NextLine");
    }

    private void InitClick()
    {
        CloseButton.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.CloseDialogue);
        NextLine.GetComponent<Button>().onClick.AddListener(DialogueManager.Instance.NextLine);
    }

    public void RefreshDialogue(string speakerName, string currentLine)
    {
        CharacterName.GetComponent<TextMeshProUGUI>().text = speakerName;
        DialogueLine.GetComponent<TextMeshProUGUI>().text = currentLine;
    }
}
