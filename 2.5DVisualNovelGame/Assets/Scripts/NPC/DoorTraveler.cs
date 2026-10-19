using System.Collections.Generic;
using UnityEngine;

// Put this on an NPC so it can follow the player through doors.
// Follow on + delay 0  = arrives with the player (companion, e.g. Stella).
// Follow on + delay 2  = bursts through the door 2 seconds later (chaser).
public class DoorTraveler : MonoBehaviour
{
    public static readonly List<DoorTraveler> All = new List<DoorTraveler>();

    [SerializeField] private bool followPlayerThroughDoors;
    [Tooltip("Seconds after the player arrives before this NPC comes through the door.")]
    [SerializeField] private float arrivalDelay;
    [Tooltip("Where to appear, relative to the door's arrival point (so we don't stand inside the player).")]
    [SerializeField] private Vector3 arrivalOffset = new Vector3(0.8f, 0f, 0f);

    private NpcMovement npcMovement;

    public bool FollowPlayer { get; set; }
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

    public void ArriveAt(Vector3 worldPosition)
    {
        if (npcMovement != null)
            npcMovement.WarpTo(worldPosition);
        else
            transform.position = worldPosition;
    }
}
