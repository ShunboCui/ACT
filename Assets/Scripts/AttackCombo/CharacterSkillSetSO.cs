using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Character Skill Set")]
public class CharacterSkillSetSO : ScriptableObject
{
    public List<SkillData> skills;
}

[Serializable]
public class SkillData
{
    public int skillId;
    public string skillName;

    public List<ComboSegment> segments;
}
