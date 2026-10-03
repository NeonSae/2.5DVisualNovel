using UnityEngine;

[CreateAssetMenu(
    fileName = "DialogueNode",
    menuName = "Dialogue/Dialogue Node"
)]
public class DialogueNode : ScriptableObject
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;

    public DialogueNode nextNode;

    public DialogueChoice[] choices;
}