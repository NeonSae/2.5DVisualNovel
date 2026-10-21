using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Opening of the game, on a black screen:
// 1. narration -> 2. doorbell / door -> 3. cutscene (pictures + dialogue)
// -> 4. door closes, fade into the house -> 5. after a while, a follow-up dialogue.
public class IntroSequence : MonoBehaviour
{
    [Header("Testing")]
    [Tooltip("Skip the whole intro (narration, cutscene, first talks): start directly with Stella going to the corridor door.")]
    [SerializeField] private bool skipIntro;
    [Tooltip("Music to play when the intro is skipped (e.g. Door Loop). Empty = no change.")]
    [SerializeField] private AudioClip musicWhenSkipping;

    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("The canvas with the dialogue box, so it can be drawn ABOVE the black screen.")]
    [SerializeField] private Canvas dialogueCanvas;
    [Tooltip("Scripts turned off during the intro (drag CharacterMovement and PlayerInteractor here).")]
    [SerializeField] private MonoBehaviour[] disableDuringIntro;

    [Header("1. Narration on black")]
    [FormerlySerializedAs("introDialogue")]
    [SerializeField] private DialogueNode narrationDialogue;
    [SerializeField] private float delayBeforeStart = 1f;

    [Header("2. Sounds before the cutscene (doorbell, door opening...)")]
    [SerializeField] private float delayBeforeSounds = 1f;
    [Tooltip("Played one after the other, in order.")]
    [SerializeField] private AudioClip[] soundsBeforeCutscene;
    [SerializeField, Range(0f, 1f)] private float soundsVolume = 1f;
    [Tooltip("Wait after each sound before the next step.")]
    [SerializeField] private float delayAfterEachSound = 1.5f;

    [Header("3. Cutscene")]
    [Tooltip("Optional: music that starts with the cutscene. (A line's own 'Start Music' also works.)")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private DialogueNode cutsceneDialogue;
    [Tooltip("Time for the last picture to fade to black.")]
    [SerializeField] private float delayAfterCutscene = 1.8f;

    [Header("4. Into the house")]
    [SerializeField] private AudioClip doorClosingSound;
    [SerializeField] private float delayAfterDoorClosing = 1f;
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("5. Follow-up dialogue (optional)")]
    [Tooltip("Starts by itself a while after the player can move.")]
    [SerializeField] private DialogueNode followUpDialogue;
    [SerializeField] private float followUpDelay = 8f;

    [Header("6. After the follow-up dialogue")]
    [Tooltip("Locked things that become usable once the follow-up dialogue is over (e.g. the corridor door).")]
    [SerializeField] private InteractionLock[] unlockAfterFollowUp;
    [Tooltip("NPC that walks somewhere after the follow-up dialogue (e.g. Stella).")]
    [SerializeField] private NpcMovement npcToMove;
    [SerializeField] private Transform npcDestination;
    [Tooltip("NPCs that come along through the NEXT door the player takes (once).")]
    [SerializeField] private DoorTraveler[] startFollowingPlayer;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (skipIntro)
            return;

        // Black from the very first frame.
        if (screenFader != null)
            screenFader.SetBlack();

        SetScriptsEnabled(false);
        IsPlaying = true;
    }

    private void Start()
    {
        if (skipIntro)
        {
            // Straight into the game: the player can move, Stella heads to the corridor door.
            AudioClip music = musicWhenSkipping != null ? musicWhenSkipping
                            : introMusic != null ? introMusic
                            : FindStartMusic(cutsceneDialogue);   // e.g. Door Loop from the cutscene
            if (music != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(music);
            AfterFollowUp();
            return;
        }

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        DrawDialogueAboveFade();

        yield return new WaitForSeconds(delayBeforeStart);

        // 1. Narration on the black screen.
        yield return PlayDialogue(narrationDialogue);

        // 2. Ding dong, door opening.
        yield return new WaitForSeconds(delayBeforeSounds);
        yield return PlaySounds(soundsBeforeCutscene, soundsVolume, delayAfterEachSound);

        // 3. Cutscene.
        if (introMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(introMusic);

        yield return PlayDialogue(cutsceneDialogue);
        yield return new WaitForSeconds(delayAfterCutscene);

        // 4. Door closes, reveal the house.
        if (doorClosingSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(doorClosingSound, soundsVolume);
            yield return new WaitForSeconds(delayAfterDoorClosing);
        }

        if (screenFader != null)
            yield return screenFader.FadeFromBlack(fadeInDuration);

        SetScriptsEnabled(true);
        IsPlaying = false;

        // 5. After a while, a dialogue starts by itself.
        if (followUpDialogue != null && dialogueManager != null)
        {
            yield return new WaitForSeconds(followUpDelay);

            // Wait until the player isn't busy (talking, inspecting, changing room).
            yield return new WaitUntil(() =>
                !dialogueManager.IsDialogueActive &&
                !InspectionUI.IsAnyOpen &&
                !RoomTransitionManager.IsAnyTransitioning);

            dialogueManager.StartDialogue(followUpDialogue);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        }

        AfterFollowUp();
    }

    // 6. The story moves on: door unlocks, Stella walks to it and follows us.
    private void AfterFollowUp()
    {
        if (unlockAfterFollowUp != null)
        {
            foreach (InteractionLock interactionLock in unlockAfterFollowUp)
            {
                if (interactionLock != null)
                    interactionLock.Unlock();
            }
        }

        if (npcToMove != null && npcDestination != null)
            npcToMove.MoveTo(npcDestination.position);

        if (startFollowingPlayer != null)
        {
            foreach (DoorTraveler traveler in startFollowingPlayer)
            {
                if (traveler != null)
                    traveler.FollowNextDoorOnly();
            }
        }
    }

    // The last music a dialogue would start (so skipping keeps the same music).
    private static AudioClip FindStartMusic(DialogueNode node)
    {
        AudioClip found = null;
        int guard = 0;
        while (node != null && guard++ < 100)
        {
            if (node.startMusic != null)
                found = node.startMusic;
            node = node.nextNode;
        }
        return found;
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

    private IEnumerator PlaySounds(AudioClip[] clips, float volume, float delayAfterEach)
    {
        if (clips == null || AudioManager.Instance == null)
            yield break;

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            AudioManager.Instance.PlaySFX(clip, volume);
            yield return new WaitForSeconds(delayAfterEach);
        }
    }

    private void DrawDialogueAboveFade()
    {
        if (dialogueCanvas == null || screenFader == null || screenFader.FadeCanvas == null)
            return;

        Canvas fadeCanvas = screenFader.FadeCanvas.rootCanvas;
        Canvas boxCanvas = dialogueCanvas.rootCanvas;

        if (boxCanvas != fadeCanvas)
        {
            // Different canvases: draw the dialogue canvas on top.
            if (boxCanvas.sortingOrder <= fadeCanvas.sortingOrder)
                boxCanvas.sortingOrder = fadeCanvas.sortingOrder + 1;
            return;
        }

        // Same canvas: UI is drawn in Hierarchy order, so move the black
        // screen to the top of the list (= drawn first, behind the dialogue box).
        Transform fadeBranch = screenFader.FadeImageTransform;
        while (fadeBranch != null && fadeBranch.parent != fadeCanvas.transform)
            fadeBranch = fadeBranch.parent;

        if (fadeBranch != null)
            fadeBranch.SetAsFirstSibling();
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
