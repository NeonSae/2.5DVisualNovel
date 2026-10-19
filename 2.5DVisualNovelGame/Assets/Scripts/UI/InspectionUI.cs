using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionUI : MonoBehaviour
{
    [Header("Simple Inspection")]
    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private TMP_Text inspectionText;

    [Header("Close-Up Inspection")]
    [SerializeField] private GameObject closeUpOverlay;
    [SerializeField] private GameObject closeUpPanel;
    [SerializeField] private Image closeUpImage;
    [SerializeField] private TMP_Text closeUpTitle;
    [SerializeField] private TMP_Text closeUpDescription;

    public bool IsOpen { get; private set; }

    // True while any inspection panel is open (used to stop the player walking).
    public static bool IsAnyOpen { get; private set; }

    // Frame the panel opened on, so the same E press doesn't close it again.
    private int openedFrame = -1;

    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void ShowTextInspection(string title, string description)
    {
        closeUpOverlay.SetActive(false);
        closeUpPanel.SetActive(false);
        inspectionPanel.SetActive(true);

        inspectionText.text = string.IsNullOrWhiteSpace(title)
            ? description
            : title + "\n\n" + description;

        IsOpen = true;
        IsAnyOpen = true;
        openedFrame = Time.frameCount;
    }

    public void ShowCloseUp(
        string title,
        string description,
        Sprite image)
    {
        inspectionPanel.SetActive(false);
        closeUpOverlay.SetActive(true);
        closeUpPanel.SetActive(true);

        closeUpTitle.text = title;
        closeUpDescription.text = description;

        closeUpImage.sprite = image;
        closeUpImage.enabled = image != null;

        IsOpen = true;
        IsAnyOpen = true;
        openedFrame = Time.frameCount;
    }

    public void Hide()
    {
        inspectionPanel.SetActive(false);
        closeUpOverlay.SetActive(false);
        closeUpPanel.SetActive(false);

        IsOpen = false;
        IsAnyOpen = false;
    }

    private void OnDestroy()
    {
        if (IsOpen)
            IsAnyOpen = false;
    }
}           