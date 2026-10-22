using UnityEngine;

// A door that is closed during some story phases (pressing E shows its Blocked Door message,
// e.g. "I shouldn't go to the kitchen right now, that's rude.") and open after.
// Set up with Tools > Sleepover > Closed Doors.
[RequireComponent(typeof(BlockedDoor))]
public class PhaseDoor : MonoBehaviour
{
    [SerializeField] private StoryPhase closedFrom = StoryPhase.StartOfGame;
    [SerializeField] private StoryPhase openedFrom = StoryPhase.Kitchen;

    private BlockedDoor blocker;

    private void Awake()
    {
        blocker = GetComponent<BlockedDoor>();
    }

    private void OnEnable()
    {
        StoryPhases.Changed += Apply;
        Apply(StoryPhases.Current);
    }

    private void OnDisable()
    {
        StoryPhases.Changed -= Apply;
    }

    private void Apply(StoryPhase phase)
    {
        bool closed = phase >= closedFrom && phase < openedFrom;
        if (closed) blocker.Block();
        else blocker.Unblock();
    }
}
