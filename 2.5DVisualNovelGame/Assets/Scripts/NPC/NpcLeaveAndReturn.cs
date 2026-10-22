using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// An NPC leaves the room, the player waits (inspecting things), then the NPC comes back
// and says something. Reusable: call Begin() (e.g. from an NPC's "On Dialogue Finished").
//
// Flow: walk to Leave Point -> go through Exit Door -> wait X seconds
// (the timer pauses while a dialogue or inspection is open) -> reappear at Return Spawn
// -> walk to Wait Point -> say Return Dialogue -> swap inspect dialogues
// -> (optional) Ready Question when talking to her / her door, until YES
// -> (optional) lead the way with her route; doors unblock once she's gone through one.
public class NpcLeaveAndReturn : MonoBehaviour
{
    [System.Serializable]
    public class DialogueSwap
    {
        public InspectDialogue target;
        [Tooltip("The new dialogue this object gets after the NPC is back.")]
        public DialogueNode newDialogue;
    }

    [Header("Who")]
    [SerializeField] private NpcMovement npc;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Leaving")]
    [SerializeField] private Transform leavePoint;
    [SerializeField] private RoomDoor exitDoor;
    [SerializeField] private AudioClip doorSound;

    [Header("Waiting")]
    [SerializeField] private float waitSeconds = 15f;
    [Tooltip("The timer stops while a dialogue box or an inspection is open.")]
    [SerializeField] private bool pauseDuringDialogue = true;

    [Header("Coming back")]
    [Tooltip("Where the NPC reappears (e.g. just inside the room's door).")]
    [SerializeField] private Transform returnSpawn;
    [Tooltip("Where the NPC stands and waits after coming back.")]
    [SerializeField] private Transform waitPoint;
    [SerializeField] private DialogueNode returnDialogue;

    [Header("Camera when coming back (optional)")]
    [Tooltip("The camera glides here when the NPC comes back (e.g. a point between the door and the room).")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private float cameraMoveTime = 1.5f;
    [Tooltip("Time to glide back to the player after the return dialogue.")]
    [SerializeField] private float cameraBackTime = 1f;

    [Header("Ready question (optional)")]
    [Tooltip("After the return dialogue, talking to the NPC or pressing E on the blocked doors asks this (e.g. 'Are you ready to go eat now?' + Yes / No).")]
    [SerializeField] private DialogueNode readyQuestion;
    [Tooltip("Which button means YES (0 = first button, 1 = second...). Any other button = nothing happens, ask again later.")]
    [SerializeField] private int yesButton = 0;
    [Tooltip("After YES, the NPC leads the way with this route (next steps of her NpcGuide).")]
    [SerializeField] private NpcGuide routeAfterYes;

    [Header("After YES (or after the return dialogue if there's no question)")]
    [Tooltip("With a route: these doors open only once the NPC has gone through a door.")]
    [SerializeField] private BlockedDoor[] unblockDoors;
    [SerializeField] private DialogueSwap[] dialogueSwaps;
    [SerializeField] private UnityEvent onFinished;

    private bool started;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    public void Begin()
    {
        if (started || npc == null)
            return;

        started = true;
        StartCoroutine(Run());
    }

    private bool PlayerIsBusy()
    {
        return (dialogueManager != null && dialogueManager.IsDialogueActive) ||
               InspectionUI.IsAnyOpen ||
               RoomTransitionManager.IsAnyTransitioning;
    }

    private IEnumerator Run()
    {
        // 1. Walk out.
        if (leavePoint != null)
            yield return WalkTo(leavePoint.position);

        PlayDoorSound();
        // The route must not start just because she went through this door.
        if (routeAfterYes != null)
            routeAfterYes.enabled = false;
        if (exitDoor != null)
            npc.UseDoor(exitDoor);

        // 2. Wait (paused while the player reads something).
        float remaining = waitSeconds;
        while (remaining > 0f)
        {
            if (!pauseDuringDialogue || !PlayerIsBusy())
                remaining -= Time.deltaTime;
            yield return null;
        }

        // 3. Come back.
        if (returnSpawn != null)
        {
            PlayDoorSound();
            npc.WarpTo(returnSpawn.position);
        }

        if (cameraPoint != null)
            CameraFocus.Instance.MoveTo(cameraPoint, cameraMoveTime);

        if (waitPoint != null)
            yield return WalkTo(waitPoint.position);

        // 4. Talk (when the player isn't busy).
        if (returnDialogue != null && dialogueManager != null)
        {
            yield return new WaitUntil(() => !PlayerIsBusy());
            dialogueManager.StartDialogue(returnDialogue);
            yield return null;
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        }

        if (cameraPoint != null)
            CameraFocus.Instance.MoveBack(cameraBackTime);

        // Objects in the room now say their new lines.
        if (dialogueSwaps != null)
            foreach (DialogueSwap swap in dialogueSwaps)
                if (swap != null && swap.target != null) swap.target.SetDialogue(swap.newDialogue);

        // 5. "Ready?" asked when the player talks to her or tries her door.
        //    Yes -> go on. No -> nothing happens, ask again any time.
        if (readyQuestion != null && dialogueManager != null)
        {
            bool saidYes = false;
            System.Action<DialogueNode, int> onChoice = (node, index) =>
            {
                if (dialogueManager.StartNode == readyQuestion && index == yesButton) saidYes = true;
            };
            dialogueManager.ChoiceSelected += onChoice;

            // The blocked doors ask the question instead of "No need to leave now".
            var oldDoorLines = new DialogueNode[unblockDoors != null ? unblockDoors.Length : 0];
            for (int i = 0; i < oldDoorLines.Length; i++)
            {
                if (unblockDoors[i] == null) continue;
                oldDoorLines[i] = unblockDoors[i].Dialogue;
                unblockDoors[i].Dialogue = readyQuestion;
            }

            NPC talk = npc.GetComponent<NPC>();
            while (!saidYes)
            {
                // Talkable again with the same question, until the player says yes.
                if (talk != null)
                    talk.SetDialogue(readyQuestion);
                yield return new WaitUntil(() => dialogueManager.IsDialogueActive);
                yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
            }
            dialogueManager.ChoiceSelected -= onChoice;

            for (int i = 0; i < oldDoorLines.Length; i++)
                if (unblockDoors[i] != null) unblockDoors[i].Dialogue = oldDoorLines[i];

            // No more talking while she leads the way.
            InteractionLock talkLock = npc.GetComponent<InteractionLock>();
            if (talkLock != null)
                talkLock.Lock();
        }

        // 6. She leads the way: the doors open once she's gone through her first door.
        if (routeAfterYes != null)
        {
            routeAfterYes.enabled = true;
            bool wentThrough = false;
            System.Action onDoor = () => wentThrough = true;
            npc.ArrivedThroughDoor += onDoor;
            routeAfterYes.ContinueRoute();
            yield return new WaitUntil(() => wentThrough);
            npc.ArrivedThroughDoor -= onDoor;
        }

        // 7. The story moves on.
        if (unblockDoors != null)
            foreach (BlockedDoor door in unblockDoors)
                if (door != null) door.Unblock();

        onFinished?.Invoke();
    }

    private IEnumerator WalkTo(Vector3 position)
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
}
