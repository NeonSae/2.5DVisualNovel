using System;
using UnityEngine;
using UnityEngine.AI;

// Moves an NPC and animates its sprite in 5 directions (left side = mirrored).
// Walking cycles through the frames; standing still shows the first frame (no idle animation).
// Works without an Animator: just drop the frames into the arrays.
public class NpcMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.1f;
    [Tooltip("Walk around walls and furniture (NavMesh). Off = straight line.")]
    [SerializeField] private bool usePathfinding = true;

    private NavMeshAgent agent;
    private bool movingWithAgent;
    private float stuckTimer;

    // Fired every time this NPC comes out of a door (used by NpcGuide).
    public event Action ArrivedThroughDoor;

    [Header("Ground")]
    [Tooltip("Keep the NPC standing on the floor (fixes floating / sinking after doors).")]
    [SerializeField] private bool snapToGround = true;
    [Tooltip("Highest step the NPC can walk up while walking. Anything taller (beds, tables, door frames) is ignored, never climbed.")]
    [SerializeField] private float stepHeight = 0.3f;
    [Tooltip("After a door/teleport, how far up we accept the floor to be (fixes arriving a bit under the floor).")]
    [SerializeField] private float arrivalStepHeight = 1f;

    // Height of the NPC above the floor, measured where it was placed in the scene.
    private float groundOffset;
    private bool hasGroundOffset;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float framesPerSecond = 8f;
    [Tooltip("When standing still, turn to face the player.")]
    [SerializeField] private bool lookAtPlayerWhenIdle;

    private Transform lookTarget;
    private float lookTimer;

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

    private void Awake()
    {
        if (usePathfinding)
            RuntimeNavMesh.EnsureBuilt();
    }

    // The NavMeshAgent is added once the NavMesh is ready (built at scene start).
    private bool TryGetAgent()
    {
        if (!usePathfinding || !RuntimeNavMesh.IsReady)
            return false;

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.updateRotation = false;      // it's a sprite, we handle facing ourselves
            agent.radius = 0.2f;
            agent.height = 1f;
            agent.baseOffset = 0f;
            agent.speed = moveSpeed;
            agent.acceleration = 30f;
            agent.angularSpeed = 720f;
            agent.stoppingDistance = 0.05f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        if (!agent.isOnNavMesh)
            agent.Warp(transform.position);

        return agent.isOnNavMesh;
    }

    private void Start()
    {
        // Remember how high above the floor the NPC was placed (like Nora's feet).
        if (TryGetGroundY(transform.position.y, 0.5f, out float groundY))
        {
            groundOffset = transform.position.y - groundY;
            hasGroundOffset = true;
        }

        ShowIdleFrame();
    }

    private void SnapToGround(float maxStepUp)
    {
        if (!snapToGround || !hasGroundOffset)
            return;

        float feetY = transform.position.y - groundOffset;
        if (TryGetGroundY(feetY + groundOffset, maxStepUp, out float groundY))
        {
            Vector3 position = transform.position;
            position.y = groundY + groundOffset;
            transform.position = position;
        }
    }

    // Finds the floor under the NPC: the highest surface that is at most
    // maxStepUp above its feet. Taller things (beds, tables, door frames) are ignored.
    private bool TryGetGroundY(float rootY, float maxStepUp, out float groundY)
    {
        groundY = 0f;
        float feetY = rootY - groundOffset;
        Vector3 origin = new Vector3(transform.position.x, feetY + 3f, transform.position.z);

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is CharacterController || hit.transform.IsChildOf(transform))
                continue;
            if (hit.point.y > feetY + maxStepUp)
                continue;

            if (!found || hit.point.y > groundY)
            {
                groundY = hit.point.y;
                found = true;
            }
        }

        return found;
    }

    // Walk to a point (keeps its own height). onArrived is called when it gets there.
    public void MoveTo(Vector3 worldPosition, Action arrived = null)
    {
        target = worldPosition;
        target.y = transform.position.y;
        onArrived = arrived;
        hasTarget = true;
        stuckTimer = 0f;

        // Pathfinding: walk around walls and furniture.
        movingWithAgent = TryGetAgent();
        if (movingWithAgent)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
            agent.SetDestination(worldPosition);
        }
    }

    // Instantly place the NPC somewhere (used by doors).
    public void WarpTo(Vector3 worldPosition)
    {
        hasTarget = false;
        onArrived = null;
        movingWithAgent = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(worldPosition);   // lands on the walkable floor
        }
        else
        {
            transform.position = worldPosition;
            SnapToGround(arrivalStepHeight);
        }

        ShowIdleFrame();
    }

    private void UpdateAgentMove()
    {
        bool closeEnough = !agent.pathPending && agent.remainingDistance <= Mathf.Max(stopDistance, agent.stoppingDistance);

        // Can't get any closer (blocked or no path)? Count it as arrived after a moment.
        if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.01f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (closeEnough || stuckTimer > 1f || agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            agent.ResetPath();
            hasTarget = false;
            movingWithAgent = false;
            ShowIdleFrame();

            Action callback = onArrived;
            onArrived = null;
            callback?.Invoke();
            return;
        }

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude > 0.01f)
        {
            UpdateFacing(velocity.normalized);
            AnimateWalk();
        }
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
        ArriveThroughDoor(destination.position);
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
        {
            LookAtPlayer();
            return;
        }

        if (movingWithAgent && agent != null && agent.isOnNavMesh)
        {
            UpdateAgentMove();
            return;
        }

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
        SnapToGround(stepHeight);

        UpdateFacing(direction);
        AnimateWalk();
    }

    private void LookAtPlayer()
    {
        if (!lookAtPlayerWhenIdle)
            return;

        lookTimer -= Time.deltaTime;
        if (lookTimer > 0f)
            return;
        lookTimer = 0.25f;

        if (lookTarget == null)
        {
            CharacterMovement player = FindFirstObjectByType<CharacterMovement>();
            if (player == null)
                return;
            lookTarget = player.transform;
        }

        FaceTowards(lookTarget.position);
    }

    // For UnityEvents: teleport to a point.
    public void WarpToPoint(Transform point)
    {
        if (point != null)
            WarpTo(point.position);
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
