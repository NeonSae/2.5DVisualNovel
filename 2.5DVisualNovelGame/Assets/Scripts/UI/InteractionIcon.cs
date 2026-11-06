using TMPro;
using UnityEngine;

// The "E" icon showing what Nora can interact with. Driven by PlayerInteractor.
// - objects & people: floats above them
// - doors: floats in front of the door, between Nora and the door
// It turns around the Y axis only (stays upright), facing the camera and leaning a bit toward Nora.
// Fades/pops in and out. Works with any children (quad, TextMeshPro "E", sprites...).
public class InteractionIcon : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Height above the top of objects / people.")]
    [SerializeField] private float heightAbove = 0.25f;
    [Tooltip("Never higher than this above Nora's feet.")]
    [SerializeField] private float maxHeightAbovePlayer = 1.6f;
    [Tooltip("Doors: how far in front of the door (toward Nora).")]
    [SerializeField] private float inFrontOfDoor = 0.4f;
    [Tooltip("Doors: height above Nora's feet.")]
    [SerializeField] private float doorIconHeight = 1.1f;

    [Header("Look")]
    [Tooltip("0 = faces the camera, 1 = fully turned toward Nora (Y axis only).")]
    [SerializeField, Range(0f, 1f)] private float leanTowardPlayer = 0.3f;
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float fadeTime = 0.15f;
    [Tooltip("Text above the E showing what E does (Inspect, Enter, Talk...). Optional.")]
    [SerializeField] private TMP_Text label;

    [Header("Locked doors")]
    [Tooltip("The 'E' letter object (hidden when the door is locked).")]
    [SerializeField] private GameObject letter;
    [Tooltip("Shown instead of the 'E' after trying a locked door (e.g. a black key).")]
    [SerializeField] private GameObject lockedIcon;
    [SerializeField] private string lockedText = "Door locked";

    public static InteractionIcon Current { get; private set; }
    private Collider lockedTarget;

    private Collider target;
    private bool targetIsDoor;
    private Transform player;
    private Vector3 baseScale;
    private Vector3 lastPosition;
    private float visible;   // 0 = hidden, 1 = fully shown

    private Renderer[] renderers;
    private TMP_Text[] texts;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        Current = this;
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        texts = GetComponentsInChildren<TMP_Text>(true);
        block = new MaterialPropertyBlock();
        ApplyFade(0f);
    }

    public void Show(Collider newTarget, Transform nora, string prompt = null)
    {
        if (newTarget == null) { Hide(); return; }
        if (newTarget != target)
        {
            lockedTarget = null;
            target = newTarget;
            targetIsDoor = newTarget.GetComponentInParent<RoomDoor>() != null ||
                           newTarget.GetComponentInParent<LockedRoomDoor>() != null;
            if (visible <= 0.01f) visible = 0f;   // pop in from nothing
        }
        player = nora;

        bool locked = lockedTarget != null && lockedTarget == target;
        if (label != null) label.text = locked ? lockedText : prompt ?? "";
        if (letter != null && letter.activeSelf == locked) letter.SetActive(!locked);
        if (lockedIcon != null && lockedIcon.activeSelf != locked) lockedIcon.SetActive(locked);
    }

    // Tried a locked door: "Door locked" + key icon until Nora leaves it.
    public void ShowLocked()
    {
        lockedTarget = target;
    }

    // Fades out where it is.
    public void Hide()
    {
        target = null;
        lockedTarget = null;
    }

    private void LateUpdate()
    {
        float goal = target != null ? 1f : 0f;
        visible = Mathf.MoveTowards(visible, goal, Time.deltaTime / Mathf.Max(0.01f, fadeTime));

        if (target != null)
            lastPosition = TargetPosition();

        if (visible <= 0f)
        {
            ApplyFade(0f);
            return;
        }

        transform.position = lastPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.rotation = UprightFacing();
        ApplyFade(Mathf.SmoothStep(0f, 1f, visible));
    }

    private Vector3 TargetPosition()
    {
        Vector3 nora = player != null ? player.position : transform.position;

        if (targetIsDoor && player != null)
        {
            // In front of the door, on Nora's side.
            Vector3 chest = nora + Vector3.up * doorIconHeight;
            Vector3 onDoor = target.ClosestPoint(chest);
            Vector3 toNora = chest - onDoor;
            toNora.y = 0f;
            if (toNora.sqrMagnitude < 0.0001f)   // Nora is inside the door's box
            {
                toNora = nora - target.bounds.center;
                toNora.y = 0f;
            }
            Vector3 p = onDoor + toNora.normalized * inFrontOfDoor;
            p.y = nora.y + doorIconHeight;
            return p;
        }

        // Above objects and people.
        Bounds b = target.bounds;
        float y = b.max.y + heightAbove;
        if (player != null) y = Mathf.Min(y, nora.y + maxHeightAbovePlayer);
        return new Vector3(b.center.x, y, b.center.z);
    }

    // Turns around Y only: faces the camera, leaning a little toward Nora.
    private Quaternion UprightFacing()
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        Quaternion facingCamera = Quaternion.LookRotation(forward.normalized, Vector3.up);

        if (player == null)
            return facingCamera;

        Vector3 fromNora = transform.position - player.position;
        fromNora.y = 0f;
        if (fromNora.sqrMagnitude < 0.0001f)
            return facingCamera;

        return Quaternion.Slerp(facingCamera, Quaternion.LookRotation(fromNora.normalized, Vector3.up), leanTowardPlayer);
    }

    private void ApplyFade(float amount)
    {
        transform.localScale = baseScale * Mathf.Lerp(0.6f, 1f, amount);

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.enabled = amount > 0f;
            if (r is SpriteRenderer sprite)
            {
                Color c = sprite.color; c.a = amount; sprite.color = c;
            }
            else if (r.GetComponent<TMP_Text>() == null && r.sharedMaterial != null)
            {
                // Quads etc.: fade the material color (needs a transparent shader, e.g. Sprites/Default).
                string prop = r.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                if (!r.sharedMaterial.HasProperty(prop)) continue;
                Color c = r.sharedMaterial.GetColor(prop); c.a *= amount;
                r.GetPropertyBlock(block);
                block.SetColor(prop, c);
                r.SetPropertyBlock(block);
            }
        }

        foreach (TMP_Text text in texts)
            if (text != null) text.alpha = amount;
    }
}
