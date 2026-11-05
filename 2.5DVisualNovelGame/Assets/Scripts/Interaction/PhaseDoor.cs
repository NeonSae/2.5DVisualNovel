using UnityEngine;

// A door that is closed during some story phases (pressing E shows a message,
// e.g. "I shouldn't go to the kitchen right now, that's rude.") and open the rest of the time.
// Set up with Tools > Sleepover > Closed Doors.
[RequireComponent(typeof(BlockedDoor))]
public class PhaseDoor : MonoBehaviour
{
    [System.Serializable]
    public class ClosedTime
    {
        public StoryPhase closedFrom = StoryPhase.Movie;
        public StoryPhase openedFrom = StoryPhase.Never;
        [Tooltip("What Nora says during this time. Empty = the Blocked Door's message.")]
        public DialogueNode message;
    }

    [Tooltip("Main closed time (uses the Blocked Door's message).")]
    [SerializeField] private StoryPhase closedFrom = StoryPhase.StartOfGame;
    [SerializeField] private StoryPhase openedFrom = StoryPhase.Kitchen;
    [Tooltip("Other times the door is closed, each with its own line (e.g. Phase 3: 'Let's go to Stella's room').")]
    [SerializeField] private ClosedTime[] moreClosedTimes;

    private BlockedDoor blocker;
    private DialogueNode mainMessage;

    private void Awake()
    {
        blocker = GetComponent<BlockedDoor>();
        mainMessage = blocker.Dialogue;
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
        // The extra times win (they have their own line).
        if (moreClosedTimes != null)
        {
            foreach (ClosedTime time in moreClosedTimes)
            {
                if (time == null || phase < time.closedFrom || phase >= time.openedFrom)
                    continue;

                blocker.Dialogue = time.message != null ? time.message : mainMessage;
                blocker.Block();
                return;
            }
        }

        blocker.Dialogue = mainMessage;
        if (phase >= closedFrom && phase < openedFrom) blocker.Block();
        else blocker.Unblock();
    }
}
