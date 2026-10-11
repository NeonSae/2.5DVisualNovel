
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine currentFade;

    public IEnumerator FadeToBlack()
    {
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeFromBlack()
    {
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
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

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
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
