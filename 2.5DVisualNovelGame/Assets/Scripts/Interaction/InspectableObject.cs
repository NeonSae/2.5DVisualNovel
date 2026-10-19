
using UnityEngine;
using static Iinteractable;

public class InspectableObject : MonoBehaviour, IInteractable
{
    public enum InspectionType
    {
        TextOnly,
        CloseUp
    }

    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Inspect";
    [SerializeField] private InspectionUI inspectionUI;

    [Header("Inspection Settings")]
    [SerializeField]
    private InspectionType inspectionType =
        InspectionType.TextOnly;

    [SerializeField] private string objectTitle = "Unknown Object";

    [TextArea(3, 8)]
    [SerializeField]
    private string description =
        "There is nothing unusual about this object.";

    [Header("Close-Up Settings")]
    [SerializeField] private Sprite closeUpSprite;

    public string InteractionPrompt => interactionPrompt;

    public void Interact()
    {
        if (inspectionUI == null)
        {
            Debug.LogWarning(
                "InspectableObject needs an InspectionUI reference.",
                this
            );
            return;
        }

        if (inspectionUI.IsOpen)
            return;

        if (inspectionType == InspectionType.TextOnly)
        {
            inspectionUI.ShowTextInspection(objectTitle, description);
        }
        else
        {
            inspectionUI.ShowCloseUp(
                objectTitle,
                description,
                closeUpSprite
            );
        }
    }
}
