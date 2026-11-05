using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Movie night, once Nora sits next to Stella (Dinner Sequence > On Player Sat > Begin):
// TV turns on -> long fade to black -> music fades out -> silence -> dialogue
// -> it's night (night lights on, day lights off) -> Nora looks left, right, hops off the bed
// -> control is back, every room is open (Phase 4: Night), night music when leaving the room.
public class MovieSequence : MonoBehaviour
{
    [Header("TV")]
    [Tooltip("The TV's renderer (the part with the screen).")]
    [SerializeField] private Renderer tvRenderer;
    [Tooltip("Which material slot of that renderer is the screen.")]
    [SerializeField] private int screenMaterialSlot;
    [Tooltip("Screen material when the TV is on. (Or use 'Tv On Texture' below.)")]
    [SerializeField] private Material tvOnMaterial;
    [Tooltip("Optional: a picture for the screen instead of a material.")]
    [SerializeField] private Texture tvOnTexture;
    [SerializeField] private float tvDelay = 1f;

    [Header("Black screen, music, dialogue")]
    [SerializeField] private ScreenFader screenFader;
    [Tooltip("The canvas with the dialogue box, so it shows on top of the black screen.")]
    [SerializeField] private Canvas dialogueCanvas;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private float fadeToBlackTime = 4f;
    [Tooltip("Wait on the black screen before the music starts fading.")]
    [SerializeField] private float delayBeforeMusicFade = 2f;
    [SerializeField] private float musicFadeTime = 3f;
    [SerializeField] private float silenceBeforeDialogue = 0.5f;
    [SerializeField] private DialogueNode movieDialogue;

    [Header("Night")]
    [Tooltip("Turned ON when the screen comes back (e.g. NightLights).")]
    [SerializeField] private GameObject[] nightObjects;
    [Tooltip("Turned OFF when the screen comes back (e.g. DayLights).")]
    [SerializeField] private GameObject[] dayObjects;
    [System.Serializable]
    public class LightChange
    {
        public Light light;
        [Tooltip("Intensity at night (e.g. Nora's light 0.7 -> 0.22).")]
        public float nightIntensity = 0.22f;
    }

    [Tooltip("Lights that get dimmer (or brighter) at night, e.g. Nora's light.")]
    [SerializeField] private LightChange[] nightLightChanges;
    [Tooltip("Gone from the house when Nora wakes up (optional).")]
    [SerializeField] private GameObject[] goneAtNight;
    [Tooltip("Stella (still on the bed, watching TV) says this when Nora talks to her at night.")]
    [SerializeField] private NPC stellaTalk;
    [SerializeField] private DialogueNode stellaNightDialogue;
    [SerializeField] private float fadeInTime = 2f;

    [Header("Nora wakes up")]
    [Tooltip("Player scripts turned back on at the end (CharacterMovement, PlayerInteractor).")]
    [SerializeField] private MonoBehaviour[] playerScripts;
    [SerializeField] private float lookTime = 0.6f;
    [Tooltip("Said after looking around, before getting off the bed (e.g. 'Damn.. I fell asleep..').")]
    [SerializeField] private DialogueNode wakeUpDialogue;
    [Tooltip("Where Nora lands after hopping off the bed.")]
    [SerializeField] private Transform standPoint;
    [SerializeField] private float hopTime = 0.35f;

    [Header("Everything open")]
    [Tooltip("Hidden doors that appear again (e.g. Stella's room exit).")]
    [SerializeField] private InteractionLock[] unlock;
    [SerializeField] private StoryPhase phaseAfter = StoryPhase.Night;
    [Tooltip("The night music starts when the player goes through this door (e.g. leaving Stella's room).")]
    [SerializeField] private RoomDoor musicDoor;
    [SerializeField] private AudioClip nightMusic;
    [SerializeField] private UnityEvent onFinished;

    private bool started;
    private bool waitingForMusicDoor;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (screenFader == null)
            screenFader = FindFirstObjectByType<ScreenFader>();
    }

    private void OnEnable() => RoomTransitionManager.PlayerArrived += OnPlayerArrived;
    private void OnDisable() => RoomTransitionManager.PlayerArrived -= OnPlayerArrived;

    public void Begin()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // 1. The TV turns on.
        yield return new WaitForSeconds(tvDelay);
        TurnTvOn();

        // 2. Long fade to black, then the music slowly goes away.
        if (screenFader != null)
            yield return screenFader.FadeToBlack(fadeToBlackTime);
        yield return new WaitForSeconds(delayBeforeMusicFade);
        if (AudioManager.Instance != null)
            AudioManager.Instance.FadeOutMusic(musicFadeTime);
        yield return new WaitForSeconds(musicFadeTime + silenceBeforeDialogue);

        // 3. Dialogue on the black screen.
        IntroSequence.DrawDialogueAboveFade(screenFader, dialogueCanvas);
        if (movieDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(movieDialogue);
            yield return null;
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
            yield return new WaitForSeconds(0.3f);
        }

        // 4. It's night now (and Stella is gone).
        SetNight();
        if (screenFader != null)
            yield return screenFader.FadeFromBlack(fadeInTime);

        // 5. Nora looks around and hops off the bed.
        yield return WakeUp();

        // 6. Back in control, the house is open.
        OpenHouse();
        onFinished?.Invoke();
    }

    private void SetNight()
    {
        SetActive(dayObjects, false);
        SetActive(nightObjects, true);
        SetActive(goneAtNight, false);

        if (nightLightChanges != null)
            foreach (LightChange change in nightLightChanges)
                if (change != null && change.light != null) change.light.intensity = change.nightIntensity;
    }

    private void OpenHouse()
    {
        if (unlock != null)
            foreach (InteractionLock l in unlock)
                if (l != null) l.Unlock();
        StoryPhases.SetPhase(phaseAfter);
        waitingForMusicDoor = musicDoor != null && nightMusic != null;

        if (stellaTalk != null && stellaNightDialogue != null)
            stellaTalk.SetDialogue(stellaNightDialogue);   // talkable again
    }

    // Testing: start the game right after the movie (night, Stella gone, house open).
    public void SkipToEnd()
    {
        started = true;
        TurnTvOn();
        SetNight();
        OpenHouse();
    }

    private IEnumerator WakeUp()
    {
        CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
        if (player == null)
            yield break;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 side = cam != null ? cam.right : Vector3.right;
        side.y = 0f;
        Vector3 p = player.transform.position;

        player.FaceTowards(p - side);   // left
        yield return new WaitForSeconds(lookTime);
        player.FaceTowards(p + side);   // right
        yield return new WaitForSeconds(lookTime);

        if (wakeUpDialogue != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(wakeUpDialogue);
            yield return null;
            yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
            yield return new WaitForSeconds(0.2f);
        }

        // Drop off the bed.
        if (standPoint != null)
        {
            Vector3 start = player.transform.position;
            Vector3 end = standPoint.position;
            player.FaceTowards(end);
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, hopTime))
            {
                player.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.25f;
                yield return null;
            }
            player.transform.position = end;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = true;
        if (playerScripts != null)
            foreach (MonoBehaviour script in playerScripts)
                if (script != null) script.enabled = true;
    }

    private void OnPlayerArrived(Transform destination)
    {
        if (!waitingForMusicDoor || destination != musicDoor.Destination)
            return;

        waitingForMusicDoor = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(nightMusic);
    }

    private void TurnTvOn()
    {
        if (tvRenderer == null)
            return;

        Material[] materials = tvRenderer.materials;   // copies, so the asset isn't changed
        if (screenMaterialSlot < 0 || screenMaterialSlot >= materials.Length)
            return;

        if (tvOnMaterial != null)
            materials[screenMaterialSlot] = tvOnMaterial;

        if (tvOnTexture != null)
        {
            Material screen = materials[screenMaterialSlot];
            if (screen.HasProperty("_BaseMap")) screen.SetTexture("_BaseMap", tvOnTexture);
            if (screen.HasProperty("_MainTex")) screen.SetTexture("_MainTex", tvOnTexture);
            if (screen.HasProperty("_EmissionMap"))
            {
                screen.EnableKeyword("_EMISSION");
                screen.SetTexture("_EmissionMap", tvOnTexture);
                screen.SetColor("_EmissionColor", Color.white);
            }
        }

        tvRenderer.materials = materials;
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject go in objects)
            if (go != null) go.SetActive(active);
    }
}
