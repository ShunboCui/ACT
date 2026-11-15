using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDPanel : BasePanel
{
    private Transform Interact;
    private Transform QuestTracker;
    private Transform PlayerHealthHUD;
    private Transform PlayerHealthSlider;
    private PlayerHealth playerHealth;

    // The HUD is always on: Esc must not close it.
    public override bool InStack
    {
        get { return false; }
    }

    override protected void Awake()
    {
        base.Awake();
        InitUI();
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
    }

    private void InitUIName()
    {
        Interact = transform.Find("Interact");
        QuestTracker = transform.Find("QuestTracker");
        PlayerHealthHUD = transform.Find("PlayerHealthHUD");
        PlayerHealthSlider = transform.Find("PlayerHealthHUD/HealthSlider");
    }

    protected override void OnOpen(object args)
    {
        playerHealth = args as PlayerHealth;
        PlayerHealthSlider.gameObject.GetComponent<PlayerHealthBar>().Bind(playerHealth);
        PlayerHealthSlider.gameObject.GetComponent<PlayerHealthBar>().UpdateHealth(playerHealth.CurrentHP, playerHealth.MaxHP);
    }

    // ---------- Interaction button (NPC prompt) ----------

    public void EnableInteractionButton()
    {
        Interact.gameObject.SetActive(true);
    }

    public void DisableInteractionButton()
    {
        Interact.gameObject.SetActive(false);
    }

    public void BindInteractionButton(DialogueData dialogueData, Func<bool> acceptQuest)
    {
        Button button = Interact.gameObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => DialogueManager.Instance.StartDialogue(dialogueData, acceptQuest));
    }
}
