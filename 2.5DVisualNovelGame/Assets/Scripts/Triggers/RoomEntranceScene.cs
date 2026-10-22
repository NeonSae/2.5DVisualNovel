using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// A little scene when the player enters a room through a door (e.g. the kitchen):
// the player can't move, an NPC is busy (walking between points, facing a spot),
// then turns toward the player for a moment and talks.
public class RoomEntranceScene : MonoBehaviour
{
    [Header("When")]
    [Tooltip("Plays when the player comes through this door.")]
    [SerializeField] private RoomDoor entranceDoor;
    [SerializeField] private bool onlyOnce = true;
    [Tooltip("Player scripts turned off during the scene (CharacterMovement, PlayerInteractor).")]
    [SerializeField] private MonoBehaviour[] disableDuringScene;

    [Header("The busy NPC")]
    [SerializeField] private NpcMovement npc;
    [Tooltip("The NPC walks between these points while busy (e.g. around the table). Empty = stands still.")]
    [SerializeField] private Transform[] busyPoints;
    [Tooltip("What the NPC looks at while busy (e.g. the table).")]
    [SerializeField] private Transform busyLookAt;
    [SerializeField] private float pauseAtEachPoint = 1.2f;

    [Header("Scene")]
    [Tooltip("The music stops when the player comes in (start new music from the dialogue's 'Start Music').")]
    [SerializeField] private bool stopMusicOnEnter = true;
    [Tooltip("Time after entering before the NPC notices the player.")]
    [SerializeField] private float delayBeforeTurning = 2f;
    [Tooltip("The NPC looks at the player this long before talking.")]
    [SerializeField] private float lookBeforeTalking = 1f;
    [SerializeField] private DialogueNode dialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private bool npcLooksAtPlayerAfter = true;

    [Header("Camera (optional)")]
    [Tooltip("The camera glides here once the room fades in (e.g. toward the table / Dad).")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private float cameraMoveTime = 2f;
    [Tooltip("Time to glide back to the player after the dialogue.")]
    [SerializeField] private float cameraBackTime = 1f;
    [SerializeField] private UnityEvent onFinished;

    private bool played;
    private bool playing;
    private Coroutine busy;
    private Transform player;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        CharacterMovement movement = FindFirstObjectByType<CharacterMovement>();
        if (movement != null)
            player = movement.transform;
    }

    private void OnEnable() => RoomTransitionManager.PlayerArrived += OnPlayerArrived;
    private void OnDisable() => RoomTransitionManager.PlayerArrived -= OnPlayerArrived;

    private void Start()
    {
        if (npc != null)
        {
            npc.LookAtPlayerWhenIdle = false;
            busy = StartCoroutine(BeBusy());
        }
    }

    private void OnPlayerArrived(Transform destination)
    {
        if (playing || (onlyOnce && played) || entranceDoor == null || destination != entranceDoor.Destination)
            return;

        StartCoroutine(Play());
    }

    // Walks between the busy points, looking at the table at each one.
    private IEnumerator BeBusy()
    {
        int index = 0;
        while (true)
        {
            if (busyPoints != null && busyPoints.Length > 0 && busyPoints[index] != null)
            {
                bool arrived = false;
                npc.MoveTo(busyPoints[index].position, () => arrived = true);
                float timeout = 10f;
                while (!arrived && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
                index = (index + 1) % busyPoints.Length;
            }

            if (busyLookAt != null)
                npc.FaceTowards(busyLookAt.position);

            yield return new WaitForSeconds(pauseAtEachPoint);
        }
    }

    private IEnumerator Play()
    {
        playing = true;
        played = true;
        SetScriptsEnabled(false);

        // Silence while the screen is still black.
        if (stopMusicOnEnter && AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // Let the screen fade in, with the NPC still busy.
        yield return new WaitUntil(() => !RoomTransitionManager.IsAnyTransitioning);

        if (cameraPoint != null)
            CameraFocus.Instance.MoveTo(cameraPoint, cameraMoveTime);

        yield return new WaitForSeconds(delayBeforeTurning);

        // Stop and turn toward the player.
        if (npc != null)
        {
            if (busy != null) StopCoroutine(busy);
            busy = null;
            npc.Stop();
            if (player != null) npc.FaceTowards(player.position);
        }

        yield return new WaitForSeconds(lookBeforeTalking);

        if (dialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(dialogue);
            yield return null;
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        }

        if (cameraPoint != null)
            CameraFocus.Instance.MoveBack(cameraBackTime);

        if (npc != null)
            npc.LookAtPlayerWhenIdle = npcLooksAtPlayerAfter;

        SetScriptsEnabled(true);
        playing = false;
        onFinished?.Invoke();
    }

    private void SetScriptsEnabled(bool enabled)
    {
        if (disableDuringScene == null)
            return;

        foreach (MonoBehaviour script in disableDuringScene)
            if (script != null) script.enabled = enabled;
    }
}
