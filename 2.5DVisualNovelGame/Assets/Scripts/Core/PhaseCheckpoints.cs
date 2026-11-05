using System.Collections;
using UnityEngine;

// TESTING: start the game directly at the phase chosen in Story Phases > "Starting Phase".
//   Start of the game  -> normal game
//   Phase 1            -> no intro, Stella walks to the corridor door (same as "Skip Intro")
//   Phase 2: Kitchen   -> you walk into the kitchen, Dad's welcome plays
//   Phase 3: Movie     -> after dinner, in Stella's room, Stella on her bed, "Sit" ready
//   Phase 4: Night     -> after the movie, night, Stella gone, every door open
// Everything before that point is marked as done. Set it back to "Start of the game" for the real game.
[DefaultExecutionOrder(-1000)]   // before the intro starts
public class PhaseCheckpoints : MonoBehaviour
{
    [SerializeField] private StoryPhases storyPhases;

    [Header("Story parts")]
    [SerializeField] private IntroSequence intro;
    [SerializeField] private NpcMovement stella;
    [SerializeField] private NpcGuide stellaGuide;
    [SerializeField] private NpcLeaveAndReturn stellaLeavesAndReturns;
    [SerializeField] private RoomEntranceScene kitchenEntrance;
    [SerializeField] private DinnerSequence dinner;
    [SerializeField] private MovieSequence movie;

    [Header("Where each checkpoint starts")]
    [Tooltip("Phase 2: Stella waits here in the kitchen.")]
    [SerializeField] private Transform stellaKitchenSpot;
    [Tooltip("Phase 2: the player comes through this door (into the kitchen).")]
    [SerializeField] private RoomDoor kitchenDoor;
    [Tooltip("Phase 3 & 4: the player comes through this door (into Stella's room).")]
    [SerializeField] private RoomDoor stellasRoomDoor;

    [Header("Music for each checkpoint (optional)")]
    [SerializeField] private AudioClip kitchenMusic;
    [SerializeField] private AudioClip movieMusic;
    [SerializeField] private AudioClip nightMusic;

    private StoryPhase start;

    private void Awake()
    {
        start = storyPhases != null ? storyPhases.StartingPhase : StoryPhase.StartOfGame;

        if (start == StoryPhase.StellasRoom && intro != null)
            intro.SkipIntroForTesting();
        else if (start >= StoryPhase.Kitchen && intro != null)
            intro.SkipEverythingForTesting();
    }

    private IEnumerator Start()
    {
        if (start < StoryPhase.Kitchen)
            yield break;

        if (start != StoryPhase.Kitchen)
            Debug.Log("[Testing] Starting at " + start + " (Story Phases > Starting Phase).");

        // Everything up to the kitchen is done.
        if (stellaLeavesAndReturns != null) stellaLeavesAndReturns.SkipToEnd();
        if (stellaGuide != null) stellaGuide.SkipAllSteps();

        // Let every other script start first.
        yield return null;

        if (start == StoryPhase.Kitchen)
        {
            if (stella != null && stellaKitchenSpot != null)
            {
                stella.WarpTo(stellaKitchenSpot.position);
                stella.LookAtPlayerWhenIdle = true;
            }
            PlayMusic(kitchenMusic);
            if (kitchenDoor != null) kitchenDoor.ForcePlayerThrough();   // Dad's welcome plays
            yield break;
        }

        // Phase 3 or 4: dinner is over.
        if (kitchenEntrance != null) kitchenEntrance.SkipForTesting();
        if (dinner != null) dinner.SkipToMovie(start >= StoryPhase.Night);

        if (start >= StoryPhase.Night)
        {
            if (movie != null) movie.SkipToEnd();
            PlayMusic(nightMusic);
        }
        else
        {
            PlayMusic(movieMusic);
        }

        if (stellasRoomDoor != null) stellasRoomDoor.ForcePlayerThrough();
    }

    private static void PlayMusic(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(clip);
    }
}
