using System.Collections;
using UnityEngine;

// Makes an NPC lead the way, room by room.
// Each time the NPC comes out of a door, the next step runs:
//   wait for the player to be close -> walk to a point -> go through a door.
// The player has to follow through the same door, and so on.
[RequireComponent(typeof(NpcMovement))]
public class NpcGuide : MonoBehaviour
{
    [System.Serializable]
    public class GuideStep
    {
        [Tooltip("Just a name for you, e.g. 'Corridor 1'.")]
        public string note;

        [Tooltip("Wait until the player is this close before moving (0 = don't wait).")]
        public float waitForPlayerWithin = 6f;
        public float waitBeforeWalking = 0.5f;

        [Tooltip("Where to walk in this room (e.g. next to the next door).")]
        public Transform walkTo;

        [Tooltip("Door to go through after arriving. Empty = stay there (last step).")]
        public RoomDoor door;
        public float waitBeforeDoor = 1f;
        public AudioClip doorSound;

        [Tooltip("Runs when the NPC reaches 'Walk To' (e.g. unlock talking to her).")]
        public UnityEngine.Events.UnityEvent onArrived;
    }

    [SerializeField] private GuideStep[] steps;

    private NpcMovement npc;
    private Transform player;
    private int nextStep;
    private bool arrivedPending;
    private Coroutine running;

    private void Awake()
    {
        npc = GetComponent<NpcMovement>();

        CharacterMovement playerMovement = FindFirstObjectByType<CharacterMovement>();
        if (playerMovement != null)
            player = playerMovement.transform;
    }

    private void OnEnable()
    {
        npc.ArrivedThroughDoor += OnArrivedThroughDoor;
    }

    private void OnDisable()
    {
        npc.ArrivedThroughDoor -= OnArrivedThroughDoor;
    }

    // Starts the next step right now (e.g. after a dialogue, from a UnityEvent).
    public void ContinueRoute()
    {
        arrivedPending = true;
    }

    private void OnArrivedThroughDoor()
    {
        // Handled in Update so we never restart a step from inside itself.
        arrivedPending = true;
    }

    private void Update()
    {
        if (!arrivedPending)
            return;

        arrivedPending = false;

        if (steps == null || nextStep >= steps.Length)
            return;

        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(RunStep(steps[nextStep]));
        nextStep++;
    }

    private IEnumerator RunStep(GuideStep step)
    {
        // 1. Wait for the player to catch up.
        if (step.waitForPlayerWithin > 0f && player != null)
        {
            yield return new WaitUntil(() =>
                Vector3.Distance(player.position, transform.position) <= step.waitForPlayerWithin &&
                !RoomTransitionManager.IsAnyTransitioning);
        }

        yield return new WaitForSeconds(step.waitBeforeWalking);

        // 2. Walk to the spot.
        if (step.walkTo != null)
        {
            bool arrived = false;
            npc.MoveTo(step.walkTo.position, () => arrived = true);
            yield return new WaitUntil(() => arrived);
        }

        step.onArrived?.Invoke();

        // 3. Go through the door (this starts the next step on the other side).
        if (step.door != null)
        {
            yield return new WaitForSeconds(step.waitBeforeDoor);

            if (step.doorSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(step.doorSound);

            running = null;
            npc.UseDoor(step.door);
        }

        running = null;
    }
}
