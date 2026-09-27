using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        // Read WASD input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        //Get the camera's horizontal directions
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y= 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // Calculate movement relative to the camera 
        Vector3 movement = forward * vertical + right * horizontal;

        // Prevent faster diagonal movement
        movement = Vector3.ClampMagnitude(movement, 1f);

        // Move and apply gravity
        controller.SimpleMove(movement * moveSpeed);
    }
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
}
