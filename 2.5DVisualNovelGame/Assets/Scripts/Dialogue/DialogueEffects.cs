using System;

[Serializable]
public class DialogueEffect
{
    public EffectType effectType;
}

public enum EffectType
{
    ToldTruthToStranger,
    LiedToStranger
}