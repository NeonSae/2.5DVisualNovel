
using System.Collections;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private Transform player;
    [SerializeField] private CharacterController characterController;

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

        // Fade out the current room.
        yield return screenFader.FadeToBlack();

        // Temporarily disable the CharacterController so repositioning
        // does not cause unwanted collision or movement.
        characterController.enabled = false;
        player.SetPositionAndRotation(
            destination.position,
            destination.rotation
        );
        characterController.enabled = true;

        // Reveal the destination room.
        yield return screenFader.FadeFromBlack();

        isTransitioning = false;
    }
}
