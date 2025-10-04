using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Yarn.Unity;

public class DialogueOption : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup; // used for fading
    public Button button;

    [Header("Timing")]
    public float duration = 3f; // how long to stay visible
    public float delay = 0f;    // how long to wait before fade in

    [Header("Yarn Settings")]
    public string nextNode;

    private DialogueRunner dialogueRunner;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (button == null)
            button = GetComponent<Button>();

        // Ensure start invisible
        canvasGroup.alpha = 0f;
        button.interactable = false;
    }

    public void Initialize(string displayText, string nodeName, DialogueRunner runner)
    {
        text.text = displayText;
        nextNode = nodeName;
        dialogueRunner = runner;

        button.onClick.AddListener(OnClicked);

        StartCoroutine(Lifecycle());
    }

    private IEnumerator Lifecycle()
    {
        // wait for delay
        yield return new WaitForSeconds(delay);

        // fade in
        yield return StartCoroutine(Fade(0f, 1f, 0.25f));
        button.interactable = true;

        // stay visible
        yield return new WaitForSeconds(duration);

        // fade out
        yield return StartCoroutine(Fade(1f, 0f, 0.25f));

        Destroy(gameObject); // fuck that hoe
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void OnClicked()
    {
        // wipe all other options
        DialogueOption[] others = Object.FindObjectsByType<DialogueOption>(FindObjectsSortMode.None);
        foreach (var opt in others)
        {
            if (opt != this)
                Destroy(opt.gameObject);
        }

        // start the next yarn node
        if (dialogueRunner != null && !string.IsNullOrEmpty(nextNode))
        {
            dialogueRunner.Stop(); // stop current line
            dialogueRunner.StartDialogue(nextNode);
        }

        // destroy self
        Destroy(gameObject);
    }
}
