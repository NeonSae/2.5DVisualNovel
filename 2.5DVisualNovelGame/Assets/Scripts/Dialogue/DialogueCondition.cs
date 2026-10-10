
using System;

[Serializable]
public class DialogueCondition
{
    public ConditionType conditionType;

    public int value;
}

public enum ConditionType
{
    ToldTruthToStranger,
    LiedToStranger,
    NPCTestTrustAtLeast,
    NPCTestTrustAtMost
}       
