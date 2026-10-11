
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionUI : MonoBehaviour
{
    [Header("Simple Inspection")]
    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private TMP_Text inspectionText;

    [Header("Close-Up Inspection")]
    [SerializeField] private GameObject closeUpPanel;
    [SerializeField] private Image closeUpImage;
    [SerializeField] private TMP_Text closeUpTitle;
    [SerializeField] private TMP_Text closeUpDescription;

    public bool IsOpen { get; private set; }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void ShowTextInspection(string title, string description)
    {
        closeUpPanel.SetActive(false);
        inspectionPanel.SetActive(true);

        inspectionText.text = string.IsNullOrWhiteSpace(title)
            ? description
            : title + "\n\n" + description;

        IsOpen = true;
    }

    public void ShowCloseUp(
        string title,
        string description,
        Sprite image)
    {
        inspectionPanel.SetActive(false);
        closeUpPanel.SetActive(true);

        closeUpTitle.text = title;
        closeUpDescription.text = description;

        closeUpImage.sprite = image;
        closeUpImage.enabled = image != null;

        IsOpen = true;
    }

    public void Hide()
    {
        inspectionPanel.SetActive(false);
        closeUpPanel.SetActive(false);

        IsOpen = false;
    }
}
