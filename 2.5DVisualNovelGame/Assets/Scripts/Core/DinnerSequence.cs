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
    [Tooltip("What Dad faces while cleaning (e.g. the sink). He doesn't look at Nora.")]
    [SerializeField] private Transform dadCleaningLookAt;
    [Tooltip("Dad's 'Talk' (NPC component): its dialogue is set here and it becomes talkable.")]
    [SerializeField] private NPC dadTalk;
    [SerializeField] private DialogueNode dadTalkDialogue;

    [Header("Watching the movie")]
    [Tooltip("Hidden (no prompt) from when Stella heads to her room (e.g. her room's exit door).")]
    [SerializeField] private InteractionLock[] hideWhileWatching;
    [Tooltip("The '[E] Sit' next to Stella (on the bed): shown once she's sitting.")]
    [SerializeField] private InteractionLock movieSitLock;
    [Tooltip("Where Nora sits, next to Stella.")]
    [SerializeField] private Transform playerMovieSeat;
    [SerializeField] private float hopTime = 0.35f;
    [Tooltip("Runs when Nora is sitting (the movie part starts here).")]
    [SerializeField] private UnityEvent onPlayerSat;

    [SerializeField] private UnityEvent onFinished;

    private bool begun;
    private bool sat;
    private bool sittingForMovie;

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
        // He's busy: he faces what he's cleaning, not Nora.
        if (dad != null && dadCleaningPoint != null)
        {
            dad.LookAtPlayerWhenIdle = false;
            dad.MoveTo(dadCleaningPoint.position, () =>
            {
                if (dadCleaningLookAt != null)
                    dad.FaceTowards(dadCleaningLookAt.position);
            });
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
        SetLocked(hideWhileWatching, true);

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
        {
            stella.SitOn(couchSeat, tv != null ? tv.position : couchSeat.position + Vector3.forward);
            yield return new WaitForSeconds(0.5f);   // let her land first
        }

        // Nora can sit next to her now.
        if (movieSitLock != null)
            movieSitLock.Unlock();
    }

    // Testing: start the game after dinner, Stella already sitting in her room.
    // sittingToo = Nora is past the "Sit" part as well (e.g. starting at night).
    public void SkipToMovie(bool sittingToo)
    {
        begun = sat = true;
        sittingForMovie = sittingToo;
        SetLocked(lockedUntilStellaLeaves, false);
        if (sitLock != null) sitLock.Lock();

        if (dad != null)
        {
            dad.LookAtPlayerWhenIdle = false;
            if (dadCleaningPoint != null) dad.WarpTo(dadCleaningPoint.position);
            if (dadCleaningLookAt != null) dad.FaceTowards(dadCleaningLookAt.position);
        }
        if (dadTalk != null && dadTalkDialogue != null)
            dadTalk.SetDialogue(dadTalkDialogue);

        SetLocked(hideWhileWatching, true);
        if (stella != null && couchSeat != null)
        {
            stella.WarpTo(couchFront != null ? couchFront.position : couchSeat.position);
            stella.SitOn(couchSeat, tv != null ? tv.position : couchSeat.position + Vector3.forward, 0.01f, 0f);
        }

        if (movieSitLock != null)
        {
            if (sittingToo) movieSitLock.Lock();
            else movieSitLock.Unlock();
        }
    }

    // Called by the bed's "[E] Sit": Nora hops next to Stella and faces the TV.
    public void SitNextToStella()
    {
        if (sittingForMovie) return;
        sittingForMovie = true;
        StartCoroutine(SitForMovie());
    }

    private IEnumerator SitForMovie()
    {
        CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
        if (player == null || playerMovieSeat == null)
            yield break;

        SetScriptsEnabled(false);   // stays seated (the movie part takes over)

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 start = player.transform.position;
        Vector3 seat = playerMovieSeat.position;
        player.FaceTowards(seat);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, hopTime))
        {
            player.transform.position = Vector3.Lerp(start, seat, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.25f;
            yield return null;
        }
        player.transform.position = seat;

        if (tv != null)
            player.FaceTowards(tv.position);

        onPlayerSat?.Invoke();
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
