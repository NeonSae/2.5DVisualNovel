using System.Collections;
using UnityEngine;

// When the player picks a certain choice (e.g. "Explore a little longer"),
// waits X seconds, then plays a reminder dialogue (e.g. "Nora? It's this way").
// The timer pauses while a dialogue / inspection is open, and is cancelled
// if the player goes through a door.
public class ChoiceReminder : MonoBehaviour
{
    [Tooltip("First line of the dialogue that has the choice.")]
    [SerializeField] private DialogueNode dialogueWithChoice;
    [Tooltip("Which button starts the timer (0 = first button, 1 = second...).")]
    [SerializeField] private int button = 0;
    [SerializeField] private float delay = 15f;
    [SerializeField] private DialogueNode reminderDialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;

    private Coroutine timer;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.ChoiceSelected += OnChoice;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.ChoiceSelected -= OnChoice;
    }

    private void OnChoice(DialogueNode node, int index)
    {
        if (dialogueManager.StartNode != dialogueWithChoice || index != button)
            return;

        if (timer != null)
            StopCoroutine(timer);
        timer = StartCoroutine(Remind());
    }

    private bool PlayerIsBusy()
    {
        return dialogueManager.IsDialogueActive || InspectionUI.IsAnyOpen;
    }

    private IEnumerator Remind()
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            // Went through a door: they're on their way, no reminder.
            if (RoomTransitionManager.IsAnyTransitioning)
                yield break;

            if (!PlayerIsBusy())
                remaining -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitUntil(() => !PlayerIsBusy() || RoomTransitionManager.IsAnyTransitioning);
        if (!RoomTransitionManager.IsAnyTransitioning && reminderDialogue != null)
            dialogueManager.StartDialogue(reminderDialogue);
        timer = null;
    }
}
