
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
    [Header("Character Portraits")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;

    [Header("Choices")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Typewriter Effect")]
    [SerializeField, Min(0.001f)]
    private float characterDelay = 0.03f;

    [Header("Cutscene Image")]
    [Tooltip("Full-screen Image placed BEHIND the dialogue box.")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private float cutsceneFadeDuration = 0.3f;

    [Header("Voice Blips")]
    [SerializeField] private AudioSource voiceSource;
    [Tooltip("One entry per character. The name must match the node's Speaker.")]
    [SerializeField] private SpeakerVoice[] speakerVoices;
    [Tooltip("Used when the speaker isn't in the list above (leave empty for silence).")]
    [SerializeField] private AudioClip defaultBlip;
    [Tooltip("Play a blip every X letters.")]
    [SerializeField, Min(1)] private int lettersPerBlip = 2;
    [SerializeField, Range(0f, 1f)] private float blipVolume = 0.6f;

    [System.Serializable]
    public class SpeakerVoice
    {
        public string speakerName;
        public AudioClip blip;
        public float minPitch = 0.95f;
        public float maxPitch = 1.05f;
    }

    private bool canContinueWithE;

    private Coroutine typingCoroutine;
    private Coroutine cutsceneFadeCoroutine;

    private AudioClip currentBlip;
    private float currentMinPitch = 1f;
    private float currentMaxPitch = 1f;

    public bool IsTyping { get; private set; }

    private void UpdatePortraits(DialogueNode node)
{
    if (leftPortrait != null)
    {
        leftPortrait.sprite = node.leftPortrait;
        leftPortrait.gameObject.SetActive(node.leftPortrait != null);
    }

    if (rightPortrait != null)
    {
        rightPortrait.sprite = node.rightPortrait;
        rightPortrait.gameObject.SetActive(node.rightPortrait != null);
    }
}
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
        HideCutsceneImage();
    }

    public void DisplayLine(DialogueNode node, bool canContinueWithE)
    {
        StopTyping();

        this.canContinueWithE = canContinueWithE;
        continueIndicator.SetActive(false);

        speakerText.text = node.speaker;
        dialogueText.text = node.text;

        UpdatePortraits(node);
        UpdateCutsceneImage(node);
        SelectVoice(node.speaker);

        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = 0;

        typingCoroutine = StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        IsTyping = true;

        int totalCharacters = dialogueText.textInfo.characterCount;
        int letterCount = 0;

        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            // Blip on letters only (not on spaces or punctuation).
            char c = dialogueText.textInfo.characterInfo[i - 1].character;
            if (char.IsLetterOrDigit(c))
            {
                if (letterCount % lettersPerBlip == 0)
                    PlayBlip();

                letterCount++;
            }

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

    // ---------- Voice blips ----------

    private void SelectVoice(string speaker)
    {
        currentBlip = defaultBlip;
        currentMinPitch = 0.95f;
        currentMaxPitch = 1.05f;

        if (speakerVoices == null || string.IsNullOrWhiteSpace(speaker))
            return;

        foreach (SpeakerVoice voice in speakerVoices)
        {
            if (voice != null &&
                voice.speakerName != null &&
                string.Equals(voice.speakerName.Trim(), speaker.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                currentBlip = voice.blip;
                currentMinPitch = voice.minPitch;
                currentMaxPitch = voice.maxPitch;
                return;
            }
        }
    }

    private void PlayBlip()
    {
        if (voiceSource == null || currentBlip == null)
            return;

        voiceSource.pitch = Random.Range(currentMinPitch, currentMaxPitch);
        voiceSource.PlayOneShot(currentBlip, blipVolume * GameSettings.SfxVolume);
    }

    // ---------- Cutscene image ----------

    private void UpdateCutsceneImage(DialogueNode node)
    {
        // Empty = keep the current picture (or keep showing nothing).
        if (cutsceneImage == null || node.cutsceneImage == null)
            return;

        if (cutsceneImage.gameObject.activeSelf && cutsceneImage.sprite == node.cutsceneImage)
            return;

        cutsceneImage.sprite = node.cutsceneImage;
        cutsceneImage.gameObject.SetActive(true);

        if (cutsceneFadeCoroutine != null)
            StopCoroutine(cutsceneFadeCoroutine);
        cutsceneFadeCoroutine = StartCoroutine(FadeCutsceneImage());
    }

    private IEnumerator FadeCutsceneImage()
    {
        Color color = cutsceneImage.color;
        float elapsed = 0f;

        while (elapsed < cutsceneFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / cutsceneFadeDuration);
            cutsceneImage.color = color;
            yield return null;
        }

        color.a = 1f;
        cutsceneImage.color = color;
        cutsceneFadeCoroutine = null;
    }

    private void HideCutsceneImage()
    {
        if (cutsceneImage == null)
            return;

        if (cutsceneFadeCoroutine != null)
        {
            StopCoroutine(cutsceneFadeCoroutine);
            cutsceneFadeCoroutine = null;
        }

        cutsceneImage.gameObject.SetActive(false);
        cutsceneImage.sprite = null;
    }

    // ---------- Choices ----------

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
