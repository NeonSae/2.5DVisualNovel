using Unity.Cinemachine;
using UnityEngine;

// Add this to a CinemachineCamera.
// Adds a gentle "breathing" bob and makes the camera slightly turn toward the mouse.
[AddComponentMenu("Cinemachine/Extensions/Menu Camera Motion")]
public class MenuCameraMotion : CinemachineExtension
{
    [Header("Bob")]
    [Tooltip("How many bobs per second.")]
    [SerializeField] private float bobSpeed = 0.35f;
    [Tooltip("How far the camera moves up and down.")]
    [SerializeField] private float bobHeight = 0.04f;
    [Tooltip("How far the camera drifts left and right.")]
    [SerializeField] private float bobSideways = 0.025f;
    [Tooltip("How much the camera tilts (degrees).")]
    [SerializeField] private float bobTilt = 0.3f;

    [Header("Mouse Follow")]
    [Tooltip("Max left/right turn toward the mouse (degrees).")]
    [SerializeField] private float maxYaw = 2.5f;
    [Tooltip("Max up/down turn toward the mouse (degrees).")]
    [SerializeField] private float maxPitch = 1.5f;
    [Tooltip("Higher = follows the mouse faster.")]
    [SerializeField] private float followSmoothing = 2.5f;

    private Vector2 smoothedMouse;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Apply after Cinemachine has finished placing the camera.
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        // Mouse position from -1 to 1 (0 = middle of the screen).
        Vector2 mouse = Vector2.zero;
        if (Screen.width > 0 && Screen.height > 0)
        {
            mouse.x = Input.mousePosition.x / Screen.width * 2f - 1f;
            mouse.y = Input.mousePosition.y / Screen.height * 2f - 1f;
            mouse.x = Mathf.Clamp(mouse.x, -1f, 1f);
            mouse.y = Mathf.Clamp(mouse.y, -1f, 1f);
        }

        // Smoothly move toward the mouse instead of snapping.
        float t = 1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime);
        smoothedMouse = Vector2.Lerp(smoothedMouse, mouse, t);

        // Bob: slow up/down wave, with a slower side drift (figure-8 feel).
        float wave = Time.unscaledTime * bobSpeed * Mathf.PI * 2f;
        Vector3 bob = new Vector3(
            Mathf.Sin(wave * 0.5f) * bobSideways,
            Mathf.Sin(wave) * bobHeight,
            0f
        );
        float tilt = Mathf.Sin(wave * 0.5f) * bobTilt;

        // Move in the camera's own directions (so "up" is camera-up).
        state.PositionCorrection += state.GetCorrectedOrientation() * bob;

        state.OrientationCorrection *= Quaternion.Euler(
            -smoothedMouse.y * maxPitch,
            smoothedMouse.x * maxYaw,
            tilt
        );
    }
}
