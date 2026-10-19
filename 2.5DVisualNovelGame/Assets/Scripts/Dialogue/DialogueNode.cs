
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

    [Header("Cutscene")]
    [Tooltip("Full-screen picture shown during this line. Leave empty to keep the previous picture (or no picture at all).")]
    public Sprite cutsceneImage;

    [Header("Dialogue Flow")]
    public DialogueNode nextNode;

    public DialogueChoice[] choices;
}
