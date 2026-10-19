using System;
using UnityEngine;

// Moves an NPC and animates its sprite in 5 directions (left side = mirrored).
// Walking cycles through the frames; standing still shows the first frame (no idle animation).
// Works without an Animator: just drop the frames into the arrays.
public class NpcMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.1f;
    [Tooltip("When going through a door, appear this far from the player's arrival spot.")]
    [SerializeField] private Vector3 doorArrivalOffset = new Vector3(0.8f, 0f, 0f);

    // Fired every time this NPC comes out of a door (used by NpcGuide).
    public event Action ArrivedThroughDoor;

    [Header("Ground")]
    [Tooltip("Keep the NPC standing on the floor (fixes floating / sinking after doors).")]
    [SerializeField] private bool snapToGround = true;
    [Tooltip("How high above the NPC we start looking for the floor.")]
    [SerializeField] private float groundCheckHeight = 2f;

    // Height of the NPC above the floor, measured where it was placed in the scene.
    private float groundOffset;
    private bool hasGroundOffset;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float framesPerSecond = 8f;

    [Header("Walk frames (Front = facing the camera)")]
    [SerializeField] private Sprite[] walkFront;
    [SerializeField] private Sprite[] walkFrontRight;
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite[] walkBackRight;
    [SerializeField] private Sprite[] walkBack;

    private bool hasTarget;
    private Vector3 target;
    private Action onArrived;

    // 0=Front, 1=FrontRight, 2=Right, 3=BackRight, 4=Back
    private int facing;
    private float frameTimer;
    private int frameIndex;

    public bool IsMoving => hasTarget;

    private void Start()
    {
        // Remember how high above the floor the NPC was placed (like Nora's feet).
        if (TryGetGroundY(transform.position, out float groundY))
        {
            groundOffset = transform.position.y - groundY;
            hasGroundOffset = true;
        }

        ShowIdleFrame();
    }

    private void SnapToGround()
    {
        if (!snapToGround || !hasGroundOffset)
            return;

        if (TryGetGroundY(transform.position, out float groundY))
        {
            Vector3 position = transform.position;
            position.y = groundY + groundOffset;
            transform.position = position;
        }
    }

    // Finds the floor under a point (ignores triggers, the player and this NPC).
    private bool TryGetGroundY(Vector3 position, out float groundY)
    {
        groundY = 0f;
        Vector3 origin = position + Vector3.up * groundCheckHeight;

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundCheckHeight + 20f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is CharacterController || hit.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                groundY = hit.point.y;
            }
        }

        return closest < float.MaxValue;
    }

    // Walk to a point (keeps its own height). onArrived is called when it gets there.
    public void MoveTo(Vector3 worldPosition, Action arrived = null)
    {
        target = worldPosition;
        target.y = transform.position.y;
        onArrived = arrived;
        hasTarget = true;
    }

    // Instantly place the NPC somewhere (used by doors).
    public void WarpTo(Vector3 worldPosition)
    {
        hasTarget = false;
        onArrived = null;
        transform.position = worldPosition;
        SnapToGround();
        ShowIdleFrame();
    }

    // Goes through the closest door (within maxDistance) and appears where it leads.
    // Returns false if no usable door was close enough.
    public bool UseNearestDoor(float maxDistance = 5f, bool ignoreLockedDoors = true)
    {
        RoomDoor nearest = null;
        float nearestDistance = maxDistance;

        foreach (RoomDoor door in FindObjectsByType<RoomDoor>(FindObjectsSortMode.None))
        {
            if (door.Destination == null)
                continue;

            if (ignoreLockedDoors)
            {
                InteractionLock doorLock = door.GetComponentInParent<InteractionLock>();
                if (doorLock != null && doorLock.IsLocked)
                    continue;
            }

            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = door;
            }
        }

        if (nearest == null)
            return false;

        return UseDoor(nearest);
    }

    // Goes through a specific door and appears where it leads
    // (next to the player's arrival spot, so they don't overlap later).
    public bool UseDoor(RoomDoor door)
    {
        if (door == null || door.Destination == null)
            return false;

        Transform destination = door.Destination;
        ArriveThroughDoor(destination.position + destination.rotation * doorArrivalOffset);
        return true;
    }

    // Appear in a new room through a door (tells NpcGuide "I arrived somewhere new").
    public void ArriveThroughDoor(Vector3 worldPosition)
    {
        WarpTo(worldPosition);
        ArrivedThroughDoor?.Invoke();
    }

    public void FaceTowards(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            UpdateFacing(direction);
        ShowIdleFrame();
    }

    private void Update()
    {
        if (!hasTarget)
            return;

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= stopDistance)
        {
            hasTarget = false;
            ShowIdleFrame();

            Action callback = onArrived;
            onArrived = null;
            callback?.Invoke();
            return;
        }

        Vector3 direction = toTarget.normalized;
        float step = Mathf.Min(moveSpeed * Time.deltaTime, toTarget.magnitude);
        transform.position += direction * step;
        SnapToGround();

        UpdateFacing(direction);
        AnimateWalk();
    }

    private void UpdateFacing(Vector3 direction)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
            return;

        // Same angles as the player's CharacterMovement.
        float x = Vector3.Dot(direction, cam.right);
        float z = Vector3.Dot(direction, cam.forward);
        float angle = Mathf.Atan2(x, -z) * Mathf.Rad2Deg;
        float absAngle = Mathf.Abs(angle);

        if (absAngle < 22.5f) facing = 0;
        else if (absAngle < 67.5f) facing = 1;
        else if (absAngle < 112.5f) facing = 2;
        else if (absAngle < 157.5f) facing = 3;
        else facing = 4;

        if (spriteRenderer != null)
            spriteRenderer.flipX = facing >= 1 && facing <= 3 && angle < 0f;
    }

    private void AnimateWalk()
    {
        Sprite[] frames = GetFrames();
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(0.01f, framesPerSecond);

        if (frameTimer >= frameTime)
        {
            frameTimer -= frameTime;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        // Skip empty slots (e.g. a frame the artist hasn't made yet).
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite frame = frames[(frameIndex + i) % frames.Length];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
                return;
            }
        }
    }

    private void ShowIdleFrame()
    {
        frameIndex = 0;
        frameTimer = 0f;

        Sprite[] frames = GetFrames();
        if (frames == null || spriteRenderer == null)
            return;

        foreach (Sprite frame in frames)
        {
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
                return;
            }
        }
    }

    private Sprite[] GetFrames()
    {
        switch (facing)
        {
            case 1: return walkFrontRight;
            case 2: return walkRight;
            case 3: return walkBackRight;
            case 4: return walkBack;
            default: return walkFront;
        }
    }
}
