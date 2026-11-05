
using UnityEngine;
using static Iinteractable;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private InspectionUI inspectionUI;

    [Header("Prompt")]
    [Tooltip("The 'E' icon shown above the object in the world.")]
    [SerializeField] private InteractionIcon worldIcon;
    [Tooltip("Also show the '[E] Interact' text at the bottom of the screen.")]
    [SerializeField] private bool showTextPrompt;

    private IInteractable currentInteractable;
    private Collider currentCollider;
    private bool waitingForERelease;
    private bool inspectionWasOpen;

    private void Update()
    {
        // Inspection is active: lock normal interactions.
        if (inspectionUI != null && inspectionUI.IsOpen)
        {
            currentInteractable = null;
            HidePrompt();

            inspectionWasOpen = true;
            waitingForERelease = true;
            return;
        }

        // Inspection just closed: don't reuse the same E press.
        if (inspectionWasOpen)
        {
            inspectionWasOpen = false;
            waitingForERelease = true;
        }

        // Preserve the existing dialogue lock.
        if (dialogueManager != null &&
            dialogueManager.IsDialogueActive)
        {
            currentInteractable = null;
            HidePrompt();

            waitingForERelease = true;
            return;
        }

        // Wait until E has been released.
        if (waitingForERelease)
        {
            if (Input.GetKeyUp(KeyCode.E))
            {
                waitingForERelease = false;
            }

            return;
        }

        FindInteractable();

        if (currentInteractable != null &&
            Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }

    private void FindInteractable()
    {
        currentInteractable = null;
        currentCollider = null;

        Vector3 origin = interactionPoint != null
            ? interactionPoint.position
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            interactionRange
        );

        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            IInteractable interactable =
                hit.GetComponentInParent<IInteractable>();

            if (interactable == null)
                continue;

            // Locked = completely ignored (no prompt, no interaction).
            InteractionLock interactionLock = hit.GetComponentInParent<InteractionLock>();
            if (interactionLock != null && interactionLock.IsLocked)
                continue;

            float distance = Vector3.Distance(
                origin,
                hit.ClosestPoint(origin)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
                currentCollider = hit;
            }
        }

        if (currentInteractable != null)
        {
            if (showTextPrompt && interactionUI != null)
                interactionUI.Show(currentInteractable.InteractionPrompt);
            if (worldIcon != null)
                worldIcon.Show(currentCollider, transform, currentInteractable.InteractionPrompt);
        }
        else
        {
            HidePrompt();
        }
    }

    private void HidePrompt()
    {
        if (interactionUI != null) interactionUI.Hide();
        if (worldIcon != null) worldIcon.Hide();
    }

    // Turned off (cutscene, sitting...): no prompt left on screen.
    private void OnDisable()
    {
        currentInteractable = null;
        HidePrompt();
    }
}
