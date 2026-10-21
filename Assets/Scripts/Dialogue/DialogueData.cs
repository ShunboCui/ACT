using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Game/DialogueData")]
public class DialogueData : ScriptableObject
{
    public string speakerName;

    [TextArea(2, 4)]
    public List<string> lines = new();
}
