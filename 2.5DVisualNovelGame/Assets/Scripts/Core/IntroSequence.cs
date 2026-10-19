using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Starts the game scene on a black screen:
// (optional narration) -> doorbell -> music -> cutscene dialogue -> fade into the house.
public class IntroSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("The canvas with the dialogue box, so it can be drawn ABOVE the black screen.")]
    [SerializeField] private Canvas dialogueCanvas;
    [Tooltip("Scripts turned off during the intro (drag CharacterMovement and PlayerInteractor here).")]
    [SerializeField] private MonoBehaviour[] disableDuringIntro;

    [Header("1. Narration on black (optional)")]
    [FormerlySerializedAs("introDialogue")]
    [SerializeField] private DialogueNode narrationDialogue;
    [SerializeField] private float delayBeforeStart = 1f;

    [Header("2. Sounds before the cutscene (doorbell, door opening...)")]
    [Tooltip("Played one after the other, in order.")]
    [SerializeField] private AudioClip[] soundsBeforeCutscene;
    [SerializeField, Range(0f, 1f)] private float soundsVolume = 1f;
    [Tooltip("Wait after each sound before the next step.")]
    [SerializeField] private float delayAfterEachSound = 1.5f;

    [Header("3. Music")]
    [Tooltip("e.g. Door Loop. Untick 'Play Music On Start' on the AudioManager when using this.")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private float delayAfterMusicStarts = 0.5f;

    [Header("4. Cutscene")]
    [SerializeField] private DialogueNode cutsceneDialogue;

    [Header("5. Fade In")]
    [SerializeField] private float delayBeforeFadeIn = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        // Black from the very first frame.
        if (screenFader != null)
            screenFader.SetBlack();

        SetScriptsEnabled(false);
        IsPlaying = true;
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        DrawDialogueAboveFade();

        yield return new WaitForSeconds(delayBeforeStart);

        // 1. Narration on the black screen.
        yield return PlayDialogue(narrationDialogue);

        // 2. Ding dong / door opening.
        if (soundsBeforeCutscene != null && AudioManager.Instance != null)
        {
            foreach (AudioClip clip in soundsBeforeCutscene)
            {
                if (clip == null)
                    continue;

                AudioManager.Instance.PlaySFX(clip, soundsVolume);
                yield return new WaitForSeconds(delayAfterEachSound);
            }
        }

        // 3. Music.
        if (introMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(introMusic);
            yield return new WaitForSeconds(delayAfterMusicStarts);
        }

        // 4. Cutscene (pictures + dialogue).
        yield return PlayDialogue(cutsceneDialogue);

        // 5. Reveal the house.
        yield return new WaitForSeconds(delayBeforeFadeIn);

        if (screenFader != null)
            yield return screenFader.FadeFromBlack(fadeInDuration);

        SetScriptsEnabled(true);
        IsPlaying = false;
    }

    private IEnumerator PlayDialogue(DialogueNode startNode)
    {
        if (startNode == null || dialogueManager == null)
            yield break;

        dialogueManager.StartDialogue(startNode);
        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);

        // Small pause so the same E press doesn't skip the next part.
        yield return new WaitForSeconds(0.3f);
    }

    private void DrawDialogueAboveFade()
    {
        if (dialogueCanvas == null || screenFader == null || screenFader.FadeCanvas == null)
            return;

        Canvas fadeCanvas = screenFader.FadeCanvas.rootCanvas;
        Canvas boxCanvas = dialogueCanvas.rootCanvas;

        if (boxCanvas != fadeCanvas && boxCanvas.sortingOrder <= fadeCanvas.sortingOrder)
            boxCanvas.sortingOrder = fadeCanvas.sortingOrder + 1;
    }

    private void SetScriptsEnabled(bool enabled)
    {
        if (disableDuringIntro == null)
            return;

        foreach (MonoBehaviour script in disableDuringIntro)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }
}
