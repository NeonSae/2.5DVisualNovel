using System;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform cameraTransform;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

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
        //Debug.Log("PlayerMovement is running!");

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
        Vector3 movement = forward * vertical + right * horizontal;

        // Prevent faster diagonal movement
        movement = Vector3.ClampMagnitude(movement, 1f);

        // Move and apply gravity
        controller.SimpleMove(movement * moveSpeed);

        if (movement.sqrMagnitude > 0.01f)
        {
            UpdateFacing(movement);
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
}