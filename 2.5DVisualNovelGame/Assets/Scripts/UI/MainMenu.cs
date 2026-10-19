using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of the game scene to load when pressing Play.")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Fade")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private bool fadeInOnStart = true;
    [Tooltip("How long the fade to black takes when pressing Play.")]
    [SerializeField] private float playFadeDuration = 1.2f;

    [Header("Walk To Door On Play (optional)")]
    [SerializeField] private CharacterMovement player;
    [Tooltip("Empty object placed where Nora should walk to.")]
    [SerializeField] private Transform doorTarget;
    [Tooltip("Always use the walking-away-from-camera animation.")]
    [SerializeField] private bool walkBackAnimation = true;
    [Tooltip("Seconds of walking before the fade starts (it starts earlier if she arrives).")]
    [SerializeField] private float fadeStartDelay = 0.8f;
    [Tooltip("Hide the buttons while Nora walks.")]
    [SerializeField] private bool hideMenuOnPlay = true;

    private bool isLoading;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);
    }

    private void Start()
    {
        CloseSettings();

        if (fadeInOnStart && screenFader != null)
            StartCoroutine(screenFader.FadeFromBlack());
    }

    private void Update()
    {
        if (settingsPanel != null &&
            settingsPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }

    public void Play()
    {
        if (isLoading)
            return;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        isLoading = true;
        SetButtonsInteractable(false);

        if (hideMenuOnPlay && mainPanel != null)
            mainPanel.SetActive(false);

        if (player != null && doorTarget != null)
        {
            // Make sure the movement script is running (it may be switched off in the menu).
            player.enabled = true;
            player.MoveTo(doorTarget.position, walkBackAnimation);

            // Let her walk a bit before fading (she keeps walking during the fade).
            float elapsed = 0f;
            while (elapsed < fadeStartDelay && player.IsMovingToTarget)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (screenFader != null)
            yield return screenFader.FadeToBlack(playFadeDuration);

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (playButton != null) playButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (quitButton != null) quitButton.interactable = interactable;
    }
}
