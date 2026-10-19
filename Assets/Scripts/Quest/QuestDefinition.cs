using UnityEngine;

[CreateAssetMenu(menuName = "Game/Quest")]
public class QuestDefinition : ScriptableObject
{
    public string questId;
    public string questName;

    [TextArea]
    public string description;

    public string targetEnemyId;
    [Min(1)] public int requiredKills = 3;

    public int rewardExp = 100;
}

public enum QuestStatus
{
    InProgress,
    ReadyToTurnIn,
    Completed
}