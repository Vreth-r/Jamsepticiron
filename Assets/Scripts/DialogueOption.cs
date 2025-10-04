using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections;

public class DialogueOption : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;
    private DialogueRunner runner;

    [Header("Timing")]
    public float duration;
    public float delay;

    private bool clicked = false;

    private string nextNode;

    // Called right after instantiation by the spawner
    public void Initialize(string displayText, string node, float d, float dur, DialogueRunner dialogueRunner)
    {
        text.text = displayText;
        nextNode = node;
        delay = d;
        duration = dur;
        runner = dialogueRunner;

        // Start fade routine
        StartCoroutine(PlayLifecycle());
    }

    private IEnumerator PlayLifecycle()
    {
        // Start invisible
        canvasGroup.alpha = 0f;

        // Wait before fade in
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        // Fade in
        float fadeTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Wait for duration (unless clicked early)
        elapsed = 0f;
        while (elapsed < duration && !clicked)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!clicked)
        {
            // Fade out if not clicked
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    public void OnClick()
    {
        if (clicked) return;

        clicked = true;

        // Destroy all other DialogueOptions on screen
        foreach (var other in FindObjectsByType<DialogueOption>(FindObjectsSortMode.None))
        {
            if (other != this)
                Destroy(other.gameObject);
        }

        // Jump Yarn
        runner?.StartDialogue(nextNode);

        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        // Always face camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180f, 0); // flip to face correctly
        }
    }
}
