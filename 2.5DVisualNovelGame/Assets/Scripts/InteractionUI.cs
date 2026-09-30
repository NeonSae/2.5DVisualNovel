using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private TMP_Text interactionText;

    public void Show(string interactionName)
    {
        prompt.SetActive(true);
        interactionText.text = "[E] " + interactionName;
    }

    public void Hide()
    {
        prompt.SetActive(false);
    }
}