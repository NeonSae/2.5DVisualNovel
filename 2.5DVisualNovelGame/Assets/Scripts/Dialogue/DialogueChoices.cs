
using System;

[Serializable]
public class DialogueChoice
{
    public string choiceText;

    public DialogueNode nextNode;

    public ConditionMode conditionMode = ConditionMode.All;

    public DialogueCondition[] conditions;

    public DialogueEffect[] effects;
}

public enum ConditionMode
{
    All,
    Any
}
