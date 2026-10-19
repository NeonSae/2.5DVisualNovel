
using UnityEngine;

[CreateAssetMenu(
    fileName = "DialogueNode",
    menuName = "Dialogue/Dialogue Node"
)]
public class DialogueNode : ScriptableObject
{
    [Header("Dialogue")]
    public string speaker;

    [TextArea(2, 5)]
    public string text;

    [Header("Character Portraits")]
    public Sprite leftPortrait;
    public Sprite rightPortrait;

    [Header("Dialogue Flow")]
    public DialogueNode nextNode;

    public DialogueChoice[] choices;
}
