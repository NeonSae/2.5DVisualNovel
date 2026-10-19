using System.Collections;
using UnityEngine;

// Box Collider (Is Trigger): when the player walks in, an NPC (e.g. Stella)
// instantly appears at a set point, after an optional delay.
[RequireComponent(typeof(Collider))]
public class NpcTeleportTrigger : MonoBehaviour
{
    [Tooltip("The NPC to teleport (e.g. Stella).")]
    [SerializeField] private Transform npc;
    [Tooltip("Where the NPC appears.")]
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private bool onlyOnce = true;
    [Tooltip("Seconds to wait after the player enters before the teleport.")]
    [SerializeField] private float delay;

    private bool hasTriggered;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterMovement>() == null)
            return;

        if ((onlyOnce && hasTriggered) || npc == null || teleportPoint == null)
            return;

        hasTriggered = true;
        StartCoroutine(TeleportAfterDelay());
    }

    private IEnumerator TeleportAfterDelay()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // NpcMovement also stops any walk in progress and resets the sprite.
        NpcMovement movement = npc.GetComponent<NpcMovement>();
        if (movement != null)
            movement.WarpTo(teleportPoint.position);
        else
            npc.position = teleportPoint.position;
    }
}
