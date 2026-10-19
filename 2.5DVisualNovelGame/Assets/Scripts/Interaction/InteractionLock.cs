using UnityEngine;

// Put this on any interactable (door, object, NPC...) to make it completely
// non-interactable while locked: no "[E]" prompt, nothing happens.
public class InteractionLock : MonoBehaviour
{
    [SerializeField] private bool lockedAtStart = true;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        IsLocked = lockedAtStart;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
    }
}
