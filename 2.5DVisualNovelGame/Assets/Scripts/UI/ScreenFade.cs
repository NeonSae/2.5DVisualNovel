
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine currentFade;

    // The canvas the fade image is drawn on.
    public Canvas FadeCanvas => fadeImage != null ? fadeImage.canvas : null;

    public Transform FadeImageTransform => fadeImage != null ? fadeImage.transform : null;

    public IEnumerator FadeToBlack()
    {
        yield return Fade(0f, 1f, fadeDuration);
    }

    public IEnumerator FadeFromBlack()
    {
        yield return Fade(1f, 0f, fadeDuration);
    }

    public IEnumerator FadeToBlack(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    // Instantly makes the screen fully black (no fade).
    public void SetBlack()
    {
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
            currentFade = null;
        }

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = startAlpha;
        fadeImage.color = color;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        if (endAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}
