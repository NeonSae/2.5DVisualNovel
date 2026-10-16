
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{

    [SerializeField] private GameObject continueIndicator;
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Choices")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Typewriter Effect")]
    [SerializeField, Min(0.001f)]

    
    private float characterDelay = 0.03f;
    private bool canContinueWithE;

    private Coroutine typingCoroutine;

    public bool IsTyping { get; private set; }

    public void Show()
    {
        dialoguePanel.SetActive(true);
    }

    public void Hide()
    {
        StopTyping();
        dialoguePanel.SetActive(false);
        continueIndicator.SetActive(false);
        ClearChoices();
    }

    public void DisplayLine(DialogueNode node, bool canContinueWithE)
    {
        StopTyping();

        this.canContinueWithE = canContinueWithE;
        continueIndicator.SetActive(false);

        speakerText.text = node.speaker;
        dialogueText.text = node.text;

        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = 0;

        typingCoroutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        IsTyping = true;

        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(characterDelay);
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;

        IsTyping = false;
        typingCoroutine = null;

        continueIndicator.SetActive(canContinueWithE);
    }

    public void CompleteTyping()
    {
        if (!IsTyping)
            return;

        StopTyping();
        dialogueText.maxVisibleCharacters = int.MaxValue;

        continueIndicator.SetActive(canContinueWithE);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        IsTyping = false;
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
