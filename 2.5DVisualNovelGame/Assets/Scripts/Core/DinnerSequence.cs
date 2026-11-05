using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// The dinner, after Dad's welcome:
// 1. Begin(): the doors stay closed, the table says "[E] Sit down".
// 2. Sit(): black screen -> everyone moves to their seat -> cinematic (pictures + music)
//    -> back in the kitchen -> "after dinner" talk.
// 3. Stella leaves through the door and goes (room by room) to sit in her room, facing the TV.
//    The doors open once she's gone. Dad goes to his spot and can be talked to.
public class DinnerSequence : MonoBehaviour
{
    [Header("Before sitting")]
    [Tooltip("Closed from Begin() until Stella has gone through the door (e.g. the kitchen door).")]
    [SerializeField] private InteractionLock[] lockedUntilStellaLeaves;
    [Tooltip("Unlocked at Begin(): the table's 'Sit down'.")]
    [SerializeField] private InteractionLock sitLock;

    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [Tooltip("The canvas with the dialogue box, so it shows on top of the black screen.")]
    [SerializeField] private Canvas dialogueCanvas;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("Player scripts turned off during the meal (CharacterMovement, PlayerInteractor).")]
    [SerializeField] private MonoBehaviour[] disableDuringMeal;

    [Header("Seats (where everyone is when the screen comes back)")]
    [SerializeField] private Transform playerSeat;
    [SerializeField] private NpcMovement stella;
    [SerializeField] private Transform stellaSeat;
    [SerializeField] private NpcMovement dad;
    [SerializeField] private Transform dadSeat;
    [Tooltip("What everyone faces at the table.")]
    [SerializeField] private Transform tableCenter;

    [Header("Cinematic")]
    [Tooltip("The dinner cutscene (lines with Cutscene Image + Start Music).")]
    [SerializeField] private DialogueNode cinematicDialogue;
    [SerializeField] private float fadeToBlackTime = 1f;
    [Tooltip("Time for the last picture to fade before the kitchen comes back.")]
    [SerializeField] private float delayAfterCinematic = 2.5f;
    [SerializeField] private float fadeInTime = 1.5f;

    [Header("After dinner")]
    [Tooltip("Back in the kitchen: Dad will clean up, Stella invites Nora to watch a movie.")]
    [SerializeField] private DialogueNode afterDinnerDialogue;

    [System.Serializable]
    public class RouteStep
    {
        [Tooltip("Walk here (next to the door)...")]
        public Transform walkTo;
        [Tooltip("...then go through this door.")]
        public RoomDoor door;
    }

    [Header("Stella goes to her seat (e.g. her room, to watch the movie)")]
    [Tooltip("Room by room. The kitchen door opens once she's gone through the FIRST door.")]
    [SerializeField] private RouteStep[] stellaRoute;
    [Tooltip("Where she stands in front of the seat before hopping on.")]
    [SerializeField] private Transform couchFront;
    [SerializeField] private Transform couchSeat;
    [SerializeField] private Transform tv;
    [SerializeField] private AudioClip doorSound;

    [Header("Dad after dinner")]
    [Tooltip("Where Dad goes to clean up (e.g. the sink).")]
    [SerializeField] private Transform dadCleaningPoint;
    [Tooltip("Dad's 'Talk' (NPC component): its dialogue is set here and it becomes talkable.")]
    [SerializeField] private NPC dadTalk;
    [SerializeField] private DialogueNode dadTalkDialogue;

    [SerializeField] private UnityEvent onFinished;

    private bool begun;
    private bool sat;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (screenFader == null)
            screenFader = FindFirstObjectByType<ScreenFader>();
    }

    // Called when Dad's welcome is over.
    public void Begin()
    {
        if (begun) return;
        begun = true;

        SetLocked(lockedUntilStellaLeaves, true);
        if (sitLock != null) sitLock.Unlock();
    }

    // Called by the table's "Sit down".
    public void Sit()
    {
        if (sat) return;
        sat = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        SetScriptsEnabled(false);

        // 1. Black screen, everyone to their seat.
        if (screenFader != null)
            yield return screenFader.FadeToBlack(fadeToBlackTime);

        PlacePlayer();
        PlaceAtTable(stella, stellaSeat);
        PlaceAtTable(dad, dadSeat);

        // 2. The cinematic, on top of the black screen.
        IntroSequence.DrawDialogueAboveFade(screenFader, dialogueCanvas);
        yield return PlayDialogue(cinematicDialogue);
        yield return new WaitForSeconds(delayAfterCinematic);

        // 3. Back in the kitchen.
        if (screenFader != null)
            yield return screenFader.FadeFromBlack(fadeInTime);

        yield return PlayDialogue(afterDinnerDialogue);
        SetScriptsEnabled(true);

        // 4. Dad goes cleaning and can be talked to.
        if (dad != null)
        {
            if (dadCleaningPoint != null)
                dad.MoveTo(dadCleaningPoint.position, () => dad.LookAtPlayerWhenIdle = true);
            else
                dad.LookAtPlayerWhenIdle = true;
        }
        if (dadTalk != null && dadTalkDialogue != null)
            dadTalk.SetDialogue(dadTalkDialogue);

        // 5. Stella speedruns to the couch.
        yield return StellaToCouch();
        onFinished?.Invoke();
    }

    private IEnumerator StellaToCouch()
    {
        if (stella == null)
        {
            SetLocked(lockedUntilStellaLeaves, false);
            yield break;
        }

        stella.LookAtPlayerWhenIdle = false;

        bool first = true;
        if (stellaRoute != null)
        {
            foreach (RouteStep step in stellaRoute)
            {
                if (step == null) continue;

                if (step.walkTo != null)
                    yield return WalkTo(stella, step.walkTo.position);

                if (step.door != null)
                {
                    if (first) PlayDoorSound();
                    stella.UseDoor(step.door);
                    yield return null;
                }

                // She's out of the kitchen: the player can leave too (or stay and look around).
                if (first)
                {
                    SetLocked(lockedUntilStellaLeaves, false);
                    first = false;
                }
            }
        }
        if (first)
            SetLocked(lockedUntilStellaLeaves, false);

        if (couchFront != null)
            yield return WalkTo(stella, couchFront.position);

        if (couchSeat != null)
            stella.SitOn(couchSeat, tv != null ? tv.position : couchSeat.position + Vector3.forward);
    }

    private void PlacePlayer()
    {
        if (playerSeat == null)
            return;

        CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
        if (player == null)
            return;

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        player.transform.position = playerSeat.position;
        if (controller != null) controller.enabled = wasEnabled;
    }

    private void PlaceAtTable(NpcMovement npc, Transform seat)
    {
        if (npc == null || seat == null)
            return;

        npc.WarpTo(seat.position);
        npc.LookAtPlayerWhenIdle = false;
        if (tableCenter != null)
            npc.FaceTowards(tableCenter.position);
    }

    private IEnumerator PlayDialogue(DialogueNode node)
    {
        if (node == null || dialogueManager == null)
            yield break;

        dialogueManager.StartDialogue(node);
        yield return null;
        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        yield return new WaitForSeconds(0.3f);   // same E press doesn't skip what's next
    }

    private static IEnumerator WalkTo(NpcMovement npc, Vector3 position)
    {
        bool arrived = false;
        npc.MoveTo(position, () => arrived = true);
        float timeout = 15f;
        while (!arrived && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
    }

    private void PlayDoorSound()
    {
        if (doorSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(doorSound);
    }

    private static void SetLocked(InteractionLock[] locks, bool locked)
    {
        if (locks == null) return;
        foreach (InteractionLock l in locks)
        {
            if (l == null) continue;
            if (locked) l.Lock();
            else l.Unlock();
        }
    }

    private void SetScriptsEnabled(bool enabled)
    {
        if (disableDuringMeal == null) return;
        foreach (MonoBehaviour script in disableDuringMeal)
            if (script != null) script.enabled = enabled;
    }
}
