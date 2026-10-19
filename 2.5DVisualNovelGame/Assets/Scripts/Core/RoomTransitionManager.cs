using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private Transform player;
    [SerializeField] private CharacterController characterController;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineConfiner3D cameraConfiner;

    [Header("Room Transition Audio")]
    [SerializeField] private AudioClip leaveRoomSound;
    [SerializeField] private AudioClip enterRoomSound;
    [SerializeField, Range(0f, 1f)] private float transitionVolume = 1f;

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    public void EnterRoom(Transform destination)
    {
        EnterRoom(destination, null);
    }

    public void EnterRoom(Transform destination, Collider roomCameraBounds)
    {
        if (isTransitioning || destination == null)
            return;

        StartCoroutine(TransitionRoutine(destination, roomCameraBounds));
    }

    private IEnumerator TransitionRoutine(Transform destination, Collider roomCameraBounds)
    {
        isTransitioning = true;

        // Sound of leaving the current room.
        PlayTransitionSound(leaveRoomSound);

        // Fade out the current room.
        yield return screenFader.FadeToBlack();

        // Temporarily disable the CharacterController while repositioning.
        characterController.enabled = false;
        player.SetPositionAndRotation(
            destination.position,
            destination.rotation
        );
        characterController.enabled = true;

        // Switch the camera to the new room's bounds.
        if (cameraConfiner != null && roomCameraBounds != null)
            cameraConfiner.BoundingVolume = roomCameraBounds;

        // Snap the camera to the player instead of sliding there.
        if (cinemachineCamera != null)
            cinemachineCamera.PreviousStateIsValid = false;

        // Give the camera one frame to move while the screen is still black.
        yield return null;

        // Sound of arriving in the destination room.
        PlayTransitionSound(enterRoomSound);

        // Reveal the destination room.
        yield return screenFader.FadeFromBlack();

        isTransitioning = false;
    }

    private void PlayTransitionSound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, transitionVolume);
    }
}