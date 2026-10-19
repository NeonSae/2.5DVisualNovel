using System.Collections;
using UnityEngine;

// Box Collider (Is Trigger): when the player walks in, an NPC (e.g. Stella)
// walks to a new spot. Optionally, after arriving, she goes through the nearest door.
[RequireComponent(typeof(Collider))]
public class NpcMoveTrigger : MonoBehaviour
{
    [SerializeField] private NpcMovement npc;
    [Tooltip("Where the NPC walks to (e.g. a new StellaDoorPoint).")]
    [SerializeField] private Transform destination;
    [SerializeField] private bool onlyOnce = true;
    [Tooltip("Seconds to wait after the player enters before the NPC starts walking.")]
    [SerializeField] private float delay;

    [Header("Use Doors")]
    [Tooltip("After arriving, the NPC goes through the closest door.")]
    [SerializeField] private bool useNearestDoorAfterArriving;
    [Tooltip("Seconds she waits at the destination before using the door.")]
    [SerializeField] private float doorDelay = 1f;
    [Tooltip("Only doors closer than this are used.")]
    [SerializeField] private float maxDoorDistance = 5f;
    [Tooltip("Optional sound when she goes through the door.")]
    [SerializeField] private AudioClip doorSound;

    private bool hasTriggered;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterMovement>() == null)
            return;

        if ((onlyOnce && hasTriggered) || npc == null || destination == null)
            return;

        hasTriggered = true;
        StartCoroutine(MoveAfterDelay());
    }

    private IEnumerator MoveAfterDelay()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        npc.MoveTo(destination.position, OnArrived);
    }

    private void OnArrived()
    {
        if (useNearestDoorAfterArriving)
            StartCoroutine(UseDoorLater());
    }

    private IEnumerator UseDoorLater()
    {
        yield return new WaitForSeconds(doorDelay);

        if (npc.UseNearestDoor(maxDoorDistance))
        {
            if (doorSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(doorSound);
        }
        else
        {
            Debug.LogWarning("No unlocked door within " + maxDoorDistance + " units of " + npc.name, this);
        }
    }
}
