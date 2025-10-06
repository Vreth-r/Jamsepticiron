using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;

    private Image fadeImage;
    private Canvas fadeCanvas;
    private Coroutine currentFade;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        // Create Canvas for fade overlay
        fadeCanvas = new GameObject("SceneFadeCanvas").AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        DontDestroyOnLoad(fadeCanvas.gameObject);

        // Add Image for fade
        fadeImage = new GameObject("FadeImage").AddComponent<Image>();
        fadeImage.transform.SetParent(fadeCanvas.transform, false);
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    public void FadeToScene(string sceneName)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeAndLoad(sceneName));
    }

    public void FadeOut(float duration = -1f)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(0f, 1f, duration > 0 ? duration : fadeDuration));
    }

    public void FadeIn(float duration = -1f)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(1f, 0f, duration > 0 ? duration : fadeDuration));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return Fade(0f, 1f, fadeDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
            yield return null;

        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        Color c = fadeColor;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }
}
