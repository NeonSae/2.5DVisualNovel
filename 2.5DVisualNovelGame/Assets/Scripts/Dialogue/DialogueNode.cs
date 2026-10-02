using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;

    public DialogueNode nextNode;

    public DialogueChoice[] choices;
}