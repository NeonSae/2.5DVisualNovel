using System;
using UnityEngine;

public enum StoryPhase
{
    [InspectorName("Start of the game")] StartOfGame = 0,
    [InspectorName("Phase 1: Stella's room")] StellasRoom = 1,
    [InspectorName("Phase 2: Kitchen")] Kitchen = 2,
    [InspectorName("Never (stays closed)")] Never = 99
}

// Knows which part of the story we're in. A phase starts when the player
// goes through a given door (e.g. leaving the living room -> Phase 1).
public class StoryPhases : MonoBehaviour
{
    [Serializable]
    public class PhaseStart
    {
        [Tooltip("Going through this door...")]
        public RoomDoor door;
        [Tooltip("...starts this phase.")]
        public StoryPhase phase;
    }

    [Tooltip("For testing: the phase the game starts in.")]
    [SerializeField] private StoryPhase startingPhase = StoryPhase.StartOfGame;
    [SerializeField] private PhaseStart[] phaseStarts;

    public static StoryPhase Current { get; private set; }
    public static event Action<StoryPhase> Changed;

    private void Awake()
    {
        SetPhase(startingPhase);
    }

    private void OnEnable() => RoomTransitionManager.PlayerArrived += OnPlayerArrived;
    private void OnDisable() => RoomTransitionManager.PlayerArrived -= OnPlayerArrived;

    private void OnPlayerArrived(Transform destination)
    {
        if (phaseStarts == null)
            return;

        foreach (PhaseStart start in phaseStarts)
        {
            // Only move forward in the story.
            if (start != null && start.door != null && start.door.Destination == destination && start.phase > Current)
                SetPhase(start.phase);
        }
    }

    public static void SetPhase(StoryPhase phase)
    {
        Current = phase;
        Changed?.Invoke(phase);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = StoryPhase.StartOfGame;
        Changed = null;
    }
}
