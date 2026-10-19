using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Makes a button grow a little when hovered and plays optional sounds.
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerClickHandler
{
    [Header("Hover Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float speed = 12f;

    [Header("Sounds (optional)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [Tooltip("Leave empty to use the AudioManager instead.")]
    [SerializeField] private AudioSource audioSource;

    private Selectable selectable;
    private Vector3 baseScale;
    private bool isHovered;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        isHovered = false;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        Vector3 target = isHovered ? baseScale * hoverScale : baseScale;
        float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, target, t);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
    public void OnPointerExit(PointerEventData eventData) => SetHovered(false);
    public void OnSelect(BaseEventData eventData) => SetHovered(true);
    public void OnDeselect(BaseEventData eventData) => SetHovered(false);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInteractable())
            PlaySound(clickSound);
    }

    private void SetHovered(bool hovered)
    {
        if (hovered && !IsInteractable())
            return;

        if (hovered && !isHovered)
            PlaySound(hoverSound);

        isHovered = hovered;
    }

    private bool IsInteractable()
    {
        return selectable == null || selectable.IsInteractable();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip, GameSettings.SfxVolume);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip);
    }
}
