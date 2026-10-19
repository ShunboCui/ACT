using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestRuntimeState
{
    public QuestDefinition quest;
    public QuestStatus status;
    public int killCount;
}

public class QuestManager
{
    public event Action<QuestDefinition> OnQuestAccepted;
    public event Action<QuestRuntimeState> OnProgressChanged;
    public event Action<QuestDefinition> OnQuestCompleted;
    private readonly Dictionary<string, QuestRuntimeState> states
        = new Dictionary<string, QuestRuntimeState>();

    public bool TryGetState(
        QuestDefinition quest,
        out QuestRuntimeState state)
    {
        state = null;

        return quest != null
            && states.TryGetValue(quest.questId, out state);
    }

    public bool AcceptQuest(QuestDefinition quest)
    {
        if (quest == null || states.ContainsKey(quest.questId))
            return false;

        var state = new QuestRuntimeState
        {
            quest = quest,
            status = QuestStatus.InProgress,
            killCount = 0
        };

        states.Add(quest.questId, state);

        OnQuestAccepted?.Invoke(quest);
        OnProgressChanged?.Invoke(state);

        return true;
    }

    public void ReportEnemyKilled(
        string questId,
        string enemyId)
    {
        if (!states.TryGetValue(questId, out var state))
            return;

        if (state.status != QuestStatus.InProgress)
            return;

        if (state.quest.targetEnemyId != enemyId)
            return;

        state.killCount++;

        if (state.killCount >= state.quest.requiredKills)
        {
            state.killCount = state.quest.requiredKills;
            state.status = QuestStatus.ReadyToTurnIn;
        }

        OnProgressChanged?.Invoke(state);
    }

    public bool CompleteQuest(QuestDefinition quest)
    {
        if (!TryGetState(quest, out var state))
            return false;

        if (state.status != QuestStatus.ReadyToTurnIn)
            return false;

        state.status = QuestStatus.Completed;

        // Player exp / reward system.
        GrantReward(quest);

        OnQuestCompleted?.Invoke(quest);
        OnProgressChanged?.Invoke(state);

        return true;
    }

    private void GrantReward(QuestDefinition quest)
    {
        // 例如：增加 quest.rewardExp 经验值；
        // 需要物品奖励时，再调用你的背包管理器。
    }
}
