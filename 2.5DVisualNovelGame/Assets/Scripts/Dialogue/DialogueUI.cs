using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;

    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button choiceButtonPrefab;

    public void Show()
    {
        dialoguePanel.SetActive(true);
    }

    public void Hide()
    {
        dialoguePanel.SetActive(false);
        ClearChoices();
    }

    public void DisplayLine(DialogueNode node)
    {
        speakerText.text = node.speaker;
        dialogueText.text = node.text;
    }

    public void DisplayChoices(
    DialogueChoice[] choices,
    System.Action<DialogueChoice> onChoiceSelected)
    {
        ClearChoices();

        if (choices == null || choices.Length == 0)
        {
            choicesPanel.SetActive(false);
            return;
        }

        choicesPanel.SetActive(true);

        for (int i = 0; i < choices.Length; i++)
        {
            DialogueChoice selectedChoice = choices[i];

            Button button = Instantiate(
                choiceButtonPrefab,
                choicesPanel.transform
            );

            TMP_Text buttonText =
                button.GetComponentInChildren<TMP_Text>();

            buttonText.text = selectedChoice.choiceText;

            button.onClick.AddListener(
                () => onChoiceSelected(selectedChoice)
            );
        }
    }

    private void ClearChoices()
    {
        foreach (Transform child in choicesPanel.transform)
        {
            Destroy(child.gameObject);
        }

        choicesPanel.SetActive(false);
    }
}