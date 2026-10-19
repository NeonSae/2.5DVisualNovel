using System.Collections;
using UnityEngine;

// Box Collider (Is Trigger) that starts a dialogue when the player walks into it.
// By default it only happens once.
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueNode dialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private bool onlyOnce = true;
    [Tooltip("Seconds to wait after entering before the dialogue starts.")]
    [SerializeField] private float delay;

    private bool hasPlayed;
    private bool isWaiting;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterMovement>() == null)
            return;

        if ((onlyOnce && hasPlayed) || isWaiting || dialogue == null || dialogueManager == null)
            return;

        StartCoroutine(PlayWhenFree());
    }

    private IEnumerator PlayWhenFree()
    {
        isWaiting = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Don't interrupt another dialogue, an inspection or a door fade.
        yield return new WaitUntil(() =>
            !dialogueManager.IsDialogueActive &&
            !InspectionUI.IsAnyOpen &&
            !RoomTransitionManager.IsAnyTransitioning);

        dialogueManager.StartDialogue(dialogue);
        hasPlayed = true;
        isWaiting = false;
    }
}
