
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
    [Tooltip("Ticked = the picture fades in. Unticked = it appears instantly.")]
    public bool fadeCutsceneImage = true;

    [Header("Audio")]
    [Tooltip("Music that starts when this line appears. Leave empty to keep the current music.")]
    public AudioClip startMusic;

    [Tooltip("Pause the music when this line appears.")]
    public bool pauseMusic;
    [Tooltip("When the paused music starts again.")]
    public MusicResumeMode resumeMusic = MusicResumeMode.WhenDialogueEnds;

    [Header("Dialogue Flow")]
    public DialogueNode nextNode;

    public DialogueChoice[] choices;
}

public enum MusicResumeMode
{
    [InspectorName("When going through a door")] WhenGoingThroughADoor,
    [InspectorName("When this line ends")] WhenThisLineEnds,
    [InspectorName("When the whole dialogue ends")] WhenDialogueEnds
}
