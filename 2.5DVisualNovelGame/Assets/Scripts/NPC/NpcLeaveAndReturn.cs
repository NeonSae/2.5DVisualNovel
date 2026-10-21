using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// An NPC leaves the room, the player waits (inspecting things), then the NPC comes back
// and says something. Reusable: call Begin() (e.g. from an NPC's "On Dialogue Finished").
//
// Flow: walk to Leave Point -> go through Exit Door -> wait X seconds
// (the timer pauses while a dialogue or inspection is open) -> reappear at Return Spawn
// -> walk to Wait Point -> say Return Dialogue -> unblock doors + swap inspect dialogues.
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

    [Header("After the return dialogue")]
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

        // 5. The story moves on.
        if (unblockDoors != null)
            foreach (BlockedDoor door in unblockDoors)
                if (door != null) door.Unblock();

        if (dialogueSwaps != null)
            foreach (DialogueSwap swap in dialogueSwaps)
                if (swap != null && swap.target != null) swap.target.SetDialogue(swap.newDialogue);

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
