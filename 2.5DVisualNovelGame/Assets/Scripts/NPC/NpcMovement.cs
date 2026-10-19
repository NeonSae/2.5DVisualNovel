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
        ShowIdleFrame();
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
        ShowIdleFrame();
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
