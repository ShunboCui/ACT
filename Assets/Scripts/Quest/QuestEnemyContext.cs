using UnityEngine;

public class QuestEnemyContext : MonoBehaviour
{

    //TODO: is this necessarily be monobehaviour?
    private QuestManager questManager;
    private string questId;
    private string enemyId;
    private bool reported;

    public void Initialize(
        QuestManager manager,
        string questId,
        string enemyId)
    {
        questManager = manager;
        this.questId = questId;
        this.enemyId = enemyId;
    }

    public void ReportDeath()
    {
        if (reported)
            return;

        reported = true;

        if (questManager != null)
        {
            questManager.ReportEnemyKilled(
                questId, enemyId);
        }
    }
}
