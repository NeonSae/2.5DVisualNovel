using System.Collections.Generic;
using UnityEngine;

// Put this on an NPC so it can follow the player through doors.
// - "Follow Player Through Doors" ticked = follows through EVERY door (chasers).
//   Delay 0 = arrives with the player, delay 2 = bursts in 2 seconds later.
// - FollowNextDoorOnly() = follows through the next door the player takes, once
//   (companions like Stella, who then lead the way with an NpcGuide).
public class DoorTraveler : MonoBehaviour
{
    public static readonly List<DoorTraveler> All = new List<DoorTraveler>();

    [SerializeField] private bool followPlayerThroughDoors;
    [Tooltip("Seconds after the player arrives before this NPC comes through the door.")]
    [SerializeField] private float arrivalDelay;
    [Tooltip("Where to appear, relative to the door's arrival point (so we don't stand inside the player).")]
    [SerializeField] private Vector3 arrivalOffset = new Vector3(0.8f, 0f, 0f);

    private NpcMovement npcMovement;

    // Follows through every door.
    public bool FollowPlayer { get; set; }

    // Follows through the next door only.
    public bool FollowOnce { get; private set; }

    public float ArrivalDelay => arrivalDelay;
    public Vector3 ArrivalOffset => arrivalOffset;

    private void Awake()
    {
        npcMovement = GetComponent<NpcMovement>();
        FollowPlayer = followPlayerThroughDoors;
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void FollowNextDoorOnly()
    {
        FollowOnce = true;
    }

    // Called by the RoomTransitionManager when the player goes through a door.
    // Returns true if this NPC should come along this time.
    public bool ShouldFollowNow()
    {
        if (FollowPlayer)
            return true;

        if (FollowOnce)
        {
            FollowOnce = false;
            return true;
        }

        return false;
    }

    public void ArriveAt(Vector3 worldPosition)
    {
        if (npcMovement != null)
            npcMovement.ArriveThroughDoor(worldPosition);
        else
            transform.position = worldPosition;
    }
}
