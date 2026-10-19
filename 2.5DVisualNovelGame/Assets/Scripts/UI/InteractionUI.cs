
using TMPro;
using UnityEngine;
using System.Collections;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private TMP_Text interactionText;

    [Header("Locked Door Message")]
    [SerializeField] private float lockedMessageDuration = 1.5f;

    private Coroutine messageCoroutine;

    public void Show(string interactionName)
    {
        // Keep the "Room Locked" message on screen until its time is up.
        if (messageCoroutine != null)
            return;

        prompt.SetActive(true);
        interactionText.text = "[E] " + interactionName;
    }

    public void ShowLockedMessage()
    {
        StopLockedMessage();
        prompt.SetActive(true);
        interactionText.text = "[E] Room Locked";

        messageCoroutine = StartCoroutine(ResetLockedMessage());
    }

    private IEnumerator ResetLockedMessage()
    {
        yield return new WaitForSeconds(lockedMessageDuration);

        messageCoroutine = null;
    }

    private void StopLockedMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }
    }

    public void Hide()
    {
        StopLockedMessage();
        prompt.SetActive(false);
    }
}
