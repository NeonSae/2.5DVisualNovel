using System.Collections;
using UnityEngine;

// Starts the game scene on a black screen: sounds + music + dialogue, then fades in.
public class IntroSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("The canvas with the dialogue box, so it can be drawn ABOVE the black screen.")]
    [SerializeField] private Canvas dialogueCanvas;
    [Tooltip("Scripts turned off during the intro (drag CharacterMovement and PlayerInteractor here).")]
    [SerializeField] private MonoBehaviour[] disableDuringIntro;

    [Header("Dialogue")]
    [SerializeField] private DialogueNode introDialogue;
    [SerializeField] private float delayBeforeDialogue = 1f;

    [Header("Audio")]
    [Tooltip("Leave empty to keep the AudioManager's normal music.")]
    [SerializeField] private AudioClip introMusic;
    [Tooltip("Played on the black screen, before the dialogue.")]
    [SerializeField] private AudioClip[] soundsBeforeDialogue;
    [Tooltip("Played after the dialogue, before the fade in (e.g. doorbell, door opening).")]
    [SerializeField] private AudioClip[] soundsAfterDialogue;
    [SerializeField] private float delayBetweenSounds = 1.2f;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Fade In")]
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

        if (introMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(introMusic);

        yield return PlaySounds(soundsBeforeDialogue);

        yield return new WaitForSeconds(delayBeforeDialogue);

        if (introDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(introDialogue);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        }

        yield return PlaySounds(soundsAfterDialogue);

        yield return new WaitForSeconds(delayBeforeFadeIn);

        if (screenFader != null)
            yield return screenFader.FadeFromBlack(fadeInDuration);

        SetScriptsEnabled(true);
        IsPlaying = false;
    }

    private IEnumerator PlaySounds(AudioClip[] clips)
    {
        if (clips == null)
            yield break;

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(clip, soundVolume);

            yield return new WaitForSeconds(delayBetweenSounds);
        }
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
