using System;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{


    [Header("Footstep Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    [SerializeField] private AudioSource footstepSource;

    private float footstepTimer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Untick to stop WASD from moving the character (e.g. in the main menu).")]
    [SerializeField] private bool playerInputEnabled = true;

    // Scripted walking (used by cutscenes / the main menu).
    private bool hasMoveTarget;
    private Vector3 moveTarget;
    private float moveStopDistance = 0.1f;
    private bool forceWalkBack;

    public bool IsMovingToTarget => hasMoveTarget;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private DialogueManager dialogueManager;

    private CharacterController controller;
        private string currentState = "";

    // 0=Front, 1=FrontRight, 2=Right ,3=Back, 4=BackRight
    //Front means facing the camera, Back means facing away from the camera
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int facingDirection = 0;
    void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            PlayAnimation("Idle");
            return;
        }
        //Debug.Log("PlayerMovement is running!");

        Vector3 movement;

        if (hasMoveTarget)
        {
            // Walk by itself toward the target point.
            Vector3 toTarget = moveTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= moveStopDistance)
            {
                hasMoveTarget = false;
                movement = Vector3.zero;
            }
            else
            {
                movement = toTarget.normalized;
            }
        }
        else if (playerInputEnabled)
        {
            // Read WASD input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            //Get the camera's horizontal directions
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            // Calculate movement relative to the camera
            movement = forward * vertical + right * horizontal;
        }
        else
        {
            movement = Vector3.zero;
        }

        // Prevent faster diagonal movement
        movement = Vector3.ClampMagnitude(movement, 1f);

        // Move and apply gravity
        if (controller.enabled)
        {
            controller.SimpleMove(movement * moveSpeed);
        }
        else
        {
            // No CharacterController (e.g. main menu): just slide the transform.
            transform.position += movement * moveSpeed * Time.deltaTime;
        }

        bool isMoving = movement.sqrMagnitude > 0.01f;

        HandleFootsteps(isMoving);

        if (isMoving)
        {
            if (hasMoveTarget && forceWalkBack)
            {
                // Always show the "walking away from the camera" animation.
                facingDirection = 4;
                spriteRenderer.flipX = false;
            }
            else
            {
                UpdateFacing(movement);
            }
            PlayAnimation("Walk");
        }
        else
        {
            PlayAnimation("Idle");
        }
    }
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // Makes the character walk by itself to a point (ignores WASD until it arrives).
    // walkBack = true always plays the "walking away from the camera" animation.
    public void MoveTo(Vector3 worldPosition, bool walkBack = false, float stopDistance = 0.1f)
    {
        moveTarget = worldPosition;
        moveStopDistance = stopDistance;
        forceWalkBack = walkBack;
        hasMoveTarget = true;
    }

    public void StopMoveTo()
    {
        hasMoveTarget = false;
    }

    private void UpdateFacing(Vector3 movement)
    {
        //Express movement relative to the camera.
        float x = Vector3.Dot(movement, cameraTransform.right);
        float z = Vector3.Dot(movement, cameraTransform.forward);

        //Positive angle is the right of the front direction
        float angle = Mathf.Atan2(x, -z) * Mathf.Rad2Deg;
        float absAngle = Mathf.Abs(angle);
        if (absAngle < 22.5f)
        {
            facingDirection = 0; //Front
        }
        else if (absAngle < 67.5f)
        {
            facingDirection = 1;
        }
        else if (absAngle < 112.5f)
        {
            facingDirection = 2; //Right 
        }
        else if (absAngle < 157.5f)
        {
            facingDirection = 3;
        }
        else
        {
            facingDirection = 4; //Back
        }

        // Flip the sprite based on the facing direction
        if (facingDirection == 1 || facingDirection == 2 || facingDirection == 3)
        {
            spriteRenderer.flipX = angle < 0f;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private void PlayAnimation(string action)
    {
        string[] directions =
    {
        "Front",
        "FrontRight",
        "Right",
        "BackRight",
        "Back"
    };

        string state = action + "_" + directions[facingDirection];

        if (state == currentState)
            return;

        if (animator == null)
        {
            //Debug.LogError("Animator is not assigned!");
            return;
        }

        if (animator.HasState(0, Animator.StringToHash(state)))
        {
            animator.Play(state, 0, 0f);
            currentState = state;
            //Debug.Log("Playing: " + state);
        }
        else
        {
            //Debug.LogError("Animator state not found: " + state);
        }
    }



    private void HandleFootsteps(bool isMoving)
    {
        if (!isMoving || footstepClips == null || footstepClips.Length == 0)
        {
            footstepTimer = 0f;

            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }

            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            // Don't start another clip while the previous footstep is playing.
            if (footstepSource != null && !footstepSource.isPlaying)
            {
                AudioClip clip = footstepClips[
                    UnityEngine.Random.Range(0, footstepClips.Length)
                ];

                if (clip != null)
                {
                    footstepSource.clip = clip;
                    footstepSource.volume = footstepVolume * GameSettings.SfxVolume;
                    footstepSource.Play();
                }
            }

            footstepTimer = footstepInterval;
        }
    }

}