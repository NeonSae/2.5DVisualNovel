using UnityEngine;
using System;

[Serializable] public class DialogueChoice
{
    public string choiceText;

    public DialogueNode nextNode;

    public DialogueCondition[] conditions;

    public DialogueEffect[] effects;
}
