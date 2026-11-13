using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestTrackerPanel : MonoBehaviour
{
    [Header("References")]
    //[SerializeField] private QuestManager questManager;

    [Header("UI")]
    [SerializeField] private GameObject panelContent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressFill;

    private QuestManager questManager;

    private QuestDefinition quest;

    private void Start()
    {
        questManager = QuestManager.Instance;
        if (questManager == null)
            return;

        QuestManager.Instance.OnProgressChanged += UpdateQuestUI;

        // Get current quest status, avoid missing previous quest
        questManager.TryGetState(quest, out var state);

        if (state != null)
        {
            UpdateQuestUI(state);
        }
        else
        {
            panelContent.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (questManager != null)
        {
            questManager.OnProgressChanged -= UpdateQuestUI;
        }
    }

    private void UpdateQuestUI(QuestRuntimeState state)
    {
        //if (state.quest.questId != quest.questId)
        //return;

        panelContent.SetActive(true);

        titleText.text = state.quest.questName;
        descriptionText.text = state.quest.description;

        progressText.text =
            $"Defeat enemies: {state.killCount}/" +
            $"{state.quest.requiredKills}";

        float progress =
            (float)state.killCount /
            state.quest.requiredKills;

        progressFill.value = Mathf.Clamp01(progress);

        switch (state.status)
        {
            case QuestStatus.InProgress:
                statusText.text = "In progress";
                break;

            case QuestStatus.ReadyToTurnIn:
                statusText.text = "Waiting for submitting";
                break;

            case QuestStatus.Completed:
                statusText.text = "Completed";
                break;
        }
    }
}