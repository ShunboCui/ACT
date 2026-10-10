using System;
using System.Collections.Generic;
using UnityEngine;

public class ComboManager
{
    [SerializeField]
    //private CharacterSkillSetSO skillSet;

    //private SkillData currentSkill;
    private List<ComboSegment> segments;

    private int currentSegmentIndex;
    private ComboSegment currentComboSegment;
    private ActionTimelineSO curentTimeline;
    private int currentLogicFrame;

    private bool comboWindow;
    //private bool nextAttackQueued;

    public bool isPlaying;
    private PlayerControl playerControl;
    private CharacterSkillSetSO characterSkillSet; 
    private float logicTimer;
    private const float LogicFrameTime = 1f / 20f;
    public float testTotalTime;

    public ComboManager(PlayerControl playerControl, CharacterSkillSetSO characterSkillSet) {
        this.playerControl = playerControl;
        this.characterSkillSet = characterSkillSet;
    }

    public void StartCombo()//int skillId)
    {
        //currentSkill = FindSkill(skillId);

        //if (currentSkill == null)
        //    return;

        currentSegmentIndex = 0;
        currentLogicFrame = 0;
        comboWindow = false;
        //nextAttackQueued = false;
        isPlaying = true;
        segments = characterSkillSet.skills[0].segments;
        PlaySegmentAnimation();
    }

    public void UpdateCombo()
    {
        testTotalTime += Time.deltaTime;

        if (!isPlaying)
            return;

        //Debug.Log("Current frame: " + currentLogicFrame);
        logicTimer += Time.deltaTime;

        while (logicTimer >= LogicFrameTime)
        {
            logicTimer -= LogicFrameTime;
            currentLogicFrame++;
        }

        currentComboSegment =
            //currentSkill.segments[currentSegmentIndex];
            segments[currentSegmentIndex];

        curentTimeline =
            currentComboSegment.timeline;

        foreach (var action in curentTimeline.frames)
        {
            if (action.frame == currentLogicFrame)
            {
                ExecuteAction(action, currentComboSegment);
            }
        }
    }

    private void ExecuteAction(ActionFrame action, ComboSegment segment)
    {
        switch (action.effect)
        {
            case ActionEffectType.Start:
                Debug.Log("Attack Start");
                break;

            case ActionEffectType.HitboxStart:
                Debug.Log("Hitbox Enabled");
                playerControl.EnableDamageArea();
                break;

            case ActionEffectType.HitboxEnd:
                Debug.Log("Hitbox Disabled");
                playerControl.DisableDamageArea();
                break;

            case ActionEffectType.ComboWindowStart:
                comboWindow = true;
                break;

            case ActionEffectType.ComboWindowEnd:
                comboWindow = false;
                break;

            case ActionEffectType.End:
                EndSegment();
                break;
        }
    }

    public void NextSegment()
    {
        if (!isPlaying)
            return;

        // Not in combo window，ignore input
        if (!comboWindow)
            return;

        // In combo window，next segment
        playerControl.DisableDamageArea();
        if (currentSegmentIndex + 1 < segments.Count)
        {
            currentSegmentIndex++;
        }
        else
        {
            currentSegmentIndex = 0;
        }

        //TODO: Disable damage area

        currentLogicFrame = 0;
        comboWindow = false;

        PlaySegmentAnimation();
        
    }

    private void PlaySegmentAnimation()
    {
        ComboSegment segment =
            //currentSkill.segments[currentSegmentIndex];
            segments[currentSegmentIndex];

        switch (currentSegmentIndex)
        {
            case 0:
                playerControl.PlayAnimation("combo_02_1");
                break;

            case 1:
                playerControl.PlayAnimation("combo_02_2");
                break;

            case 2:
                playerControl.PlayAnimation("combo_02_3");
                break;

            case 3:
                playerControl.PlayAnimation("combo_02_4");
                break;
        }
    }

    private void EndSegment()
    {
        EndCombo();
    }

    public void EndCombo()
    {
        isPlaying = false;
        Debug.Log("Combo End");
    }

    public int GetLogicFrame()
    {
        return currentLogicFrame;
    }
}

[Serializable]
public class ComboSegment
{
    public int segmentId;

    public ActionTimelineSO timeline;

    public int damage;
}