using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity.Attributes;
using TMPro;
using Yarn.Unity;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
public class CustomOptionsPresenter : DialoguePresenterBase
{
    // ripped from OptionsPresenter

    [SerializeField] CanvasGroup? canvasGroup;

    [MustNotBeNull]
    [SerializeField] OptionItem? optionViewPrefab;
    public Transform? optionViewParent;
    public float swayAmplitude = 10f;
    public float swaySpeed = 3f;

    [SerializeField] private DialogueRunner? runner;

    // A cached pool of OptionView objects so that we can reuse them
    List<OptionItem> optionViews = new List<OptionItem>();

    /// <summary>
    /// Controls whether or not to display options whose <see
    /// cref="OptionSet.Option.IsAvailable"/> value is <see
    /// langword="false"/>.
    /// </summary>
    [Space]
    public bool showUnavailableOptions = false;

    [Group("Fade")]
    [Label("Fade UI")]
    public bool useFadeEffect = true;

    [Group("Fade")]
    [ShowIf(nameof(useFadeEffect))]
    public float fadeUpDuration = 0.25f;

    [Group("Fade")]
    [ShowIf(nameof(useFadeEffect))]
    public float fadeDownDuration = 0.1f;

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to dismiss the options view
    /// when dialogue is complete.
    /// </summary>
    /// <returns>A completed task.</returns>

    private void Awake()
    {
        if (runner == null)
            runner = FindFirstObjectByType<DialogueRunner>();
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by Unity to set up the object.
    /// </summary>
    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> to set up the options view
    /// when dialogue begins.
    /// </summary>
    /// <returns>A completed task.</returns>
    public override YarnTask OnDialogueStartedAsync()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by a <see cref="DialogueRunner"/> when a line needs to be
    /// presented, and stores the line as the 'last seen line' so that it
    /// can be shown when options appear.
    /// </summary>
    /// <remarks>This view does not display lines directly, but instead
    /// stores lines so that when options are run, the last line that ran
    /// before the options appeared can be shown.</remarks>
    /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync"
    /// path="/param"/>
    /// <returns>A completed task.</returns>
    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        return YarnTask.CompletedTask;
    }

    public static T ParseValue<T>(string input)
    {
        var parts = input.Split(':', 2);
        if (parts.Length < 2)
            Debug.LogError("Input must be in the format 'parameter:value'.");

        string valuePart = parts[1].Trim();
        return (T)Convert.ChangeType(valuePart, typeof(T));
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
    {
        // Ensure enough option views exist
        while (dialogueOptions.Length-1 > optionViews.Count)
            optionViews.Add(CreateNewOptionView());

        // a view never gets created for the last dialogue option in the yarn script, use that for passing secret vars
        DialogueOption defaultOption = dialogueOptions[^1]; // might add optionality later

        var selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();
        var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async YarnTask CancelSourceWhenDialogueCancelled()
        {
            await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

            if (cancellationToken.IsCancellationRequested == true)
            {
                // The overall cancellation token was fired, not just our
                // internal 'something was selected' cancellation token.
                // This means that the dialogue view has been informed that
                // any value it returns will not be used. Set a 'null'
                // result on our completion source so that that we can get
                // out of here as quickly as possible.
                selectedOptionCompletionSource.TrySetResult(null);
            }
        }

        // Start waiting 
        CancelSourceWhenDialogueCancelled().Forget();


        for (int i = 0; i < dialogueOptions.Length-1; i++)
        {
            var optionView = optionViews[i];
            var option = dialogueOptions[i];

            if (!option.IsAvailable && !showUnavailableOptions)
                continue;

            optionView.Option = option;
            optionView.OnOptionSelected = selectedOptionCompletionSource;
            optionView.completionToken = completionCancellationSource.Token;
            optionView.gameObject.SetActive(true);
        }

        float maxOptionDisplayTime = 0f; // default
        if (runner.VariableStorage.TryGetValue("$optionsTimer", out float val))
        {
            if (float.TryParse(val.ToString(), out float time))
                maxOptionDisplayTime = time;
        }

        StartCoroutine(GlobalTimeoutCoroutine(maxOptionDisplayTime, selectedOptionCompletionSource, completionCancellationSource));

        // bug fix
        int firstIndex = -1;
        for (int i = 0; i < optionViews.Count; i++)
        {
            var view = optionViews[i];
            if (!view.isActiveAndEnabled) continue;
            if (firstIndex == -1) firstIndex = i;
            if (view.IsHighlighted)
            {
                firstIndex = i;
                break;
            }
        }
        if (firstIndex >= 0)
            optionViews[firstIndex].Select();

        foreach (var view in optionViews)
        {
            // Start lifecycle coroutine, call OnOptionFinished at the end
            StartCoroutine(OptionLifecycleCoroutine(view, completionCancellationSource.Token));
        }

        // Wait for a selection or all options to finish
        var completedOption = await selectedOptionCompletionSource.Task;
        Debug.Log(completedOption);
        completionCancellationSource.Cancel();

        // Cleanup UI
        foreach (var view in optionViews)
            SafeHideOption(view);

        await YarnTask.Yield();

        // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
        if (cancellationToken.IsCancellationRequested || completedOption == null)
        {
            Debug.Log("No Option Selected");
            return defaultOption;
        }

        Debug.Log($"returning option: {completedOption}");
        return completedOption;
    }

    // --- Coroutine for the global timeout ---
    private System.Collections.IEnumerator GlobalTimeoutCoroutine(
        float timeoutSeconds,
        YarnTaskCompletionSource<DialogueOption?> tcs,
        CancellationTokenSource cts)
    {
        Debug.Log("Fire Coroutine");
        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (cts.IsCancellationRequested)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Coroutine Timer Done");
        // Timeout expired, set null if nothing selected
        tcs.TrySetResult(null);
        //cts.Cancel();
    }

    private System.Collections.IEnumerator OptionLifecycleCoroutine(OptionItem view, CancellationToken token)
    {
        var cg = view.GetComponent<CanvasGroup>();
        if (cg == null) cg = view.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // --- 1) Read metadata ---
        string[] meta = view.Option.Line.Metadata;
        float delay = 0f, duration = 3f;
        string vPos = "center", hPos = "center";

        if (meta != null)
        {
            if (meta.Length > 0 && !string.IsNullOrEmpty(meta[0])) delay = ParseValue<float>(meta[0]);
            if (meta.Length > 1 && !string.IsNullOrEmpty(meta[1])) duration = ParseValue<float>(meta[1]);
            if (meta.Length > 2 && !string.IsNullOrEmpty(meta[2])) vPos = ParseValue<string>(meta[2]);
            if (meta.Length > 3 && !string.IsNullOrEmpty(meta[3])) hPos = ParseValue<string>(meta[3]);
        }

        // --- 2) Position ---
        float x = hPos.ToLowerInvariant() switch { "left" => -65f, "center" => 0f, "right" => 65f, _ => 0f };
        float y = vPos.ToLowerInvariant() switch { "top" => 15f, "center" => 0f, "bottom" => -20f, _ => 0f };
        Vector3 basePos = new Vector3(x, y, 0f);
        view.transform.localPosition = basePos;
        SwayAndFaceCamera swayScript = view.gameObject.GetComponent<SwayAndFaceCamera>();
        swayScript.SetBasePos(basePos);
        swayScript.working = true;

        // --- 3) Wait delay ---
        float waited = 0f;
        while (waited < delay)
        {
            if (token.IsCancellationRequested) yield break;
            waited += Time.deltaTime;
            yield return null;
        }

        // --- Fade in ---
        float t = 0f;
        while (t < fadeUpDuration)
        {
            if (token.IsCancellationRequested) break;
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeUpDuration);
            yield return null;
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // --- Active duration ---
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (token.IsCancellationRequested) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- Fade out ---
        t = 0f;
        while (t < fadeDownDuration)
        {
            if (token.IsCancellationRequested) break;
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDownDuration);
            yield return null;
        }

        cg.alpha = 0f;
        view.gameObject.SetActive(false);
    }


    private void SafeHideOption(OptionItem view)
    {
        if (view == null) return;
        var cg = view.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        view.gameObject.SetActive(false);
    }

    private OptionItem CreateNewOptionView()
    {
        var optionView = Instantiate(optionViewPrefab);

        if (optionView == null)
        {
            throw new System.InvalidOperationException($"Can't create new option view: {nameof(optionView)} is null");
        }

        optionView.transform.SetParent(optionViewParent, false);
        optionView.transform.SetAsLastSibling();
        optionView.gameObject.SetActive(false);

        return optionView;
    }
}

