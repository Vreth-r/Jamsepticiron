/*
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueOption : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    private string nextNode;
    private bool interactable = false;

    public void Setup(string displayText, string node, float delay, float duration)
    {
        text.text = displayText;
        nextNode = node;

        // Ensure proper initial state
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        StartCoroutine(Lifecycle(delay, duration));
    }

    private IEnumerator Lifecycle(float delay, float duration)
    {
        // Wait before fade in
        yield return new WaitForSeconds(delay);

        // Fade in and enable raycasts only when visible
        yield return Fade(0f, 1f);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        interactable = true;

        // Stay visible for the duration
        yield return new WaitForSeconds(duration);

        // Fade out and disable raycasts again
        interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        yield return Fade(1f, 0f);
        Destroy(gameObject);
    }

    public void OnClick()
    {
        if (!interactable) return;

        interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Debug.Log("Dialogue option clicked");
        GameManager.Instance.OnOptionClicked(nextNode);
    }

    public IEnumerator Fade(float from, float to, float time = 0.5f)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }

        canvasGroup.alpha = to;

        // Adjust raycast blocking dynamically at end of fade
        if (to <= 0f)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    public void FadeOutAndDestroy() => StartCoroutine(FadeAndKill());

    private IEnumerator FadeAndKill()
    {
        interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        yield return Fade(canvasGroup.alpha, 0f);
        Destroy(gameObject);
    }
}
*/
