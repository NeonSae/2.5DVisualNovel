using UnityEngine;
using static Iinteractable;

public class PlayerInteractor : MonoBehaviour
{

    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private InteractionUI interactionUI;

    private IInteractable currentInteractable;

    private void Update()
    {
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

            float distance = Vector3.Distance(
                origin,
                hit.ClosestPoint(origin)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
            }
        }

        // Update the UI
        if (currentInteractable != null)
        {
            interactionUI.Show(currentInteractable.InteractionPrompt);
        }
        else
        {
            interactionUI.Hide();
        }
    }
}