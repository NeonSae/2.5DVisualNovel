using UnityEngine;
using System;

[Serializable]
public class DialogueCondition
{
    public ConditionType conditionType;
}

public enum ConditionType
{
    ToldTruthToStranger,
    LiedToStranger
}