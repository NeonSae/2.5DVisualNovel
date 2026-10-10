using UnityEngine;
using System;

[Serializable]
public class DialogueEffect
{
    public EffectType effectType;

    public int value;
}

public enum EffectType
{
    ToldTruthToStranger,
    LiedToStranger,
    ChangeNPCTestTrust
}
