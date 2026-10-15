
using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private Transform player;
    [SerializeField] private CharacterController characterController;

    [Header("Room Transition Audio")]
    [SerializeField] private AudioClip leaveRoomSound;
    [SerializeField] private AudioClip enterRoomSound;
    [SerializeField, Range(0f, 1f)] private float transitionVolume = 1f;

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    public void EnterRoom(Transform destination)
    {
        if (isTransitioning || destination == null)
            return;

        StartCoroutine(TransitionRoutine(destination));
    }

    private IEnumerator TransitionRoutine(Transform destination)
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
