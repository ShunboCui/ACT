using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Action Timeline")]
public class ActionTimelineSO : ScriptableObject
{
    public int totalFrames;

    public List<ActionFrame> frames;
}

[Serializable]
public class ActionFrame
{
    public int frame;
    public ActionEffectType effect;
}

public enum ActionEffectType
{
    Start,
    HitboxStart,
    HitboxEnd,
    ComboWindowStart,
    ComboWindowEnd,
    End
}