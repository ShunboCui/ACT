using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestSpawnConfig
{
    public string questId;
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int spawnCount = 3;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private List<QuestSpawnConfig> configs;

    private readonly HashSet<string> spawnedQuests
        = new HashSet<string>();


    private void OnEnable()
    {
        if (questManager != null)
            questManager.OnQuestAccepted += SpawnForQuest;
    }

    private void OnDisable()
    {
        if (questManager != null)
            questManager.OnQuestAccepted -= SpawnForQuest;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void SpawnForQuest(QuestDefinition quest)
    {
        if (spawnedQuests.Contains(quest.questId))
            return;

        var config = configs.Find(
            x => x.questId == quest.questId);

        if (config == null
            || config.enemyPrefab == null
            || config.spawnPoints == null
            || config.spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                $"任务 {quest.questId} 没有有效的生成配置");
            return;
        }

        // 保证生成数量足够完成击杀目标。
        int count = Mathf.Max(
            config.spawnCount, quest.requiredKills);

        spawnedQuests.Add(quest.questId);

        for (int i = 0; i < count; i++)
        {
            Transform point =
                config.spawnPoints[i % config.spawnPoints.Length];

            GameObject enemy = Instantiate(
                config.enemyPrefab,
                point.position,
                point.rotation);

            var context = enemy.GetComponent<QuestEnemyContext>();

            if (context != null)
            {
                context.Initialize(
                    questManager,
                    quest.questId,
                    quest.targetEnemyId);
            }
            else
            {
                Debug.LogError(
                    "敌人 Prefab 缺少 QuestEnemyContext");
            }
        }
    }
}
