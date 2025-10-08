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
        // 1) Ensure enough option views exist
        while (dialogueOptions.Length > optionViews.Count)
        {
            var optionView = CreateNewOptionView();
            optionViews.Add(optionView);
        }

        // 2) Completion source for selected option
        var selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();

        // 3) Linked cancellation token
        var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 4) Handle global cancellation
        async YarnTask WatchForCancellation()
        {
            await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);
            if (cancellationToken.IsCancellationRequested)
                selectedOptionCompletionSource.TrySetResult(null);
        }
        WatchForCancellation().Forget();

        // 5) Configure each option
        for (int i = 0; i < dialogueOptions.Length; i++)
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

        // 6) Select first/highlighted option
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

        // 7) Optional canvas fade in
        if (useFadeEffect && canvasGroup != null)
            await Effects.FadeAlphaAsync(canvasGroup, 0f, 1f, fadeUpDuration, cancellationToken);

        // 8) Start each option’s lifecycle independently
        foreach (var view in optionViews)
        {
            StartCoroutine(OptionViewCoroutine(view, completionCancellationSource.Token));
        }

        // 9) Wait for selection
        var completedTask = await selectedOptionCompletionSource.Task;
        completionCancellationSource.Cancel();

        // 10) Fade out canvas
        if (useFadeEffect && canvasGroup != null)
            await Effects.FadeAlphaAsync(canvasGroup, 1f, 0f, fadeDownDuration, cancellationToken);

        // 11) Cleanup all option views
        foreach (var view in optionViews)
            SafeHideOption(view);

        await YarnTask.Yield();

        // 12) Return selected option or None
        if (cancellationToken.IsCancellationRequested)
            return await DialogueRunner.NoOptionSelected;

        return completedTask;
    }

    /*
    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
    {
        // If we don't already have enough option views, create more
        while (dialogueOptions.Length > optionViews.Count)
        {
            var optionView = CreateNewOptionView();
            optionViews.Add(optionView);
        }

        // A completion source that represents the selected option.
        YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();

        // A cancellation token source that becomes cancelled when any
        // option item is selected, or when this entire option view is
        // cancelled
        var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async YarnTask CancelSourceWhenDialogueCancelled()
        {
            await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

            // SET TO AXE
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

        // SET TO AXE
        // Start waiting 
        CancelSourceWhenDialogueCancelled().Forget();

        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            // for all the options
            var optionView = optionViews[i]; // get its option view
            var option = dialogueOptions[i]; // get the option

            if (option.IsAvailable == false && showUnavailableOptions == false)
            {
                // option is unavailable, skip it
                continue;
            }
            // else
            optionView.gameObject.SetActive(true);
            optionView.Option = option; // set the option to the view

            optionView.OnOptionSelected = selectedOptionCompletionSource;
            optionView.completionToken = completionCancellationSource.Token;
        }

        // There is a bug that can happen where in-between option items being configured one can be selected
        // and because the items are still being configured the others don't get the deselect message
        // which means visually two items are selected.
        // So instead now after configuring them we find if any are highlighted, and if so select that one
        // otherwise select the first non-deactivated one
        // because at this point now all of them are configured they will all get the select/deselect message
        int optionIndexToSelect = -1;
        for (int i = 0; i < optionViews.Count; i++)
        {
            var view = optionViews[i];
            if (!view.isActiveAndEnabled)
            {
                continue;
            }

            if (view.IsHighlighted)
            {
                optionIndexToSelect = i;
                break;
            }

            // ok at this point the view is enabled
            // but not highlighted
            // so if we haven't already decreed we have found one to select
            // we select this one
            if (optionIndexToSelect == -1)
            {
                optionIndexToSelect = i;
            }
        }
        if (optionIndexToSelect > -1)
        {
            optionViews[optionIndexToSelect].Select();
        }

        if (useFadeEffect && canvasGroup != null)
        {
            // fade up the UI now
            await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, cancellationToken);
            // ^ dont really need to do that because the option views are instantiated under a different canvas
        }

        // Start each option lifecycle in parallel, should run independently
        foreach (var view in optionViews)
        {
            ShowOption(view, completionCancellationSource.Token);
        }

        // allow interactivity and wait for an option to be selected
        // this simply does not matter with the subparenting.
        /*
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

    // Wait for a selection to be made, or for the task to be completed.
    var completedTask = await selectedOptionCompletionSource.Task;
        completionCancellationSource.Cancel();

        // now one of the option items has been selected so we do cleanup
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (useFadeEffect && canvasGroup != null)
        {
            // fade down
            await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, cancellationToken);
        }

        // disabling ALL the options views now
        foreach (var optionView in optionViews)
        {
            optionView.gameObject.SetActive(false);
        }
        await YarnTask.Yield();

        // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
        if (cancellationToken.IsCancellationRequested)
        {
            return await DialogueRunner.NoOptionSelected;
        }

        // finally we return the selected option
        return completedTask;
    }
    */
    /*
    private async YarnTask ShowOptionAsync(OptionItem view, CancellationToken cancellationToken)
    {
        if (view == null) return;

        try
        {
            // --- 1) Read metadata safely ---
            // metadata entries may not exist; treat them defensively
            string[] meta = view.Option.Line.Metadata;
            float delay = 0f;
            float duration = 3f;
            string vPos = "center";
            string hPos = "center";

            if (meta != null)
            {
                if (meta.Length > 0 && !string.IsNullOrEmpty(meta[0]))
                {
                    delay = ParseValue<float>(meta[0]);
                }
                if (meta.Length > 1 && !string.IsNullOrEmpty(meta[1]))
                {
                    duration = ParseValue<float>(meta[1]);
                }
                if (meta.Length > 2 && !string.IsNullOrEmpty(meta[2]))
                {
                    vPos = ParseValue<string>(meta[2]);
                }
                if (meta.Length > 3 && !string.IsNullOrEmpty(meta[3]))
                {
                    hPos = ParseValue<string>(meta[3]);
                }
            }

            // --- 2) Position the option ---
            // Map keywords to relative positions
            float x;
            float y;
            switch (hPos.ToLowerInvariant())
            {
                case "left": x = -65f; break;
                case "center": x = 0f; break;
                case "right": x = 65f; break;
                default:
                    x = 0f; break;
            }

            switch (vPos.ToLowerInvariant())
            {
                case "top": y = 15f; break;
                case "center": y = 0f; break;
                case "bottom": y = -20f; break;
                default:
                    y = 0f; break;
            }


            // set localPosition in world-space canvases (preserve z)
            view.gameObject.transform.localPosition = new Vector3(x, y, 0f);
            Vector3 position = new Vector3(x, y, 0f);

            view.gameObject.SetActive(true);
            // sway and camera facing
            var cam = Camera.main;
            float elapsed = 0f;

            // --- 4) Fade in ---
            var cg = view.gameObject.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            while (elapsed < duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;

                // face cam
                view.transform.LookAt(cam.transform);
                view.transform.Rotate(0f, 180f, 0f);
                //view.gameObject.transform.rotation = Quaternion.LookRotation(view.gameObject.transform.rotation - cam.transform.rotation);

                float swayOffset = Mathf.Sin((Time.time + GetInstanceID() * 0.1f) * swaySpeed) * swayAmplitude;
                view.gameObject.transform.localPosition = position + new Vector3(0f, swayOffset, 0f);

                await YarnTask.Yield();
            }

            // --- Wait the per-option delay ---
            if (delay > 0f)
            {
                await YarnTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }

            if (cg != null)
            {
                // Fade in
                await Effects.FadeAlphaAsync(cg, 0f, 1f, fadeUpDuration, cancellationToken);
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            // Wait for the display duration or until selection/cancel
            float timeVisible = 0f;
            while (timeVisible < duration && !cancellationToken.IsCancellationRequested)
            {
                timeVisible += Time.deltaTime;
                await YarnTask.Yield();
            }

            // Fade out
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
                await Effects.FadeAlphaAsync(cg, 1f, 0f, fadeDownDuration, cancellationToken);
            }

            view.gameObject.SetActive(false);
        }
        catch (OperationCanceledException) // YarnTask.Delay -> throws if cancelled
        {
            // cancelled; ensure the option doesn't block anything
            try
            {
                var cg = view.gameObject.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
                view.gameObject.SetActive(false);
            }
            catch { }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ShowOptionAsync error: {ex}");
        }
    }
    */
    private System.Collections.IEnumerator OptionViewCoroutine(OptionItem view, CancellationToken token)
    {
        if (view == null || view.Option == null)
            yield break;

        var cg = view.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = view.gameObject.AddComponent<CanvasGroup>();

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
        swayScript.enable = true;
        // --- 3) Wait delay ---
        float waited = 0f;
        while (waited < delay)
        {
            if (token.IsCancellationRequested) yield break;
            waited += Time.deltaTime;
            yield return null;
        }

        // --- 4) Fade in ---
        float t = 0f;
        while (t < fadeUpDuration)
        {
            if (token.IsCancellationRequested) yield break;
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeUpDuration);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // --- 5) Active duration ---
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (token.IsCancellationRequested) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- 6) Fade out ---
        cg.interactable = false;
        cg.blocksRaycasts = false;
        t = 0f;
        while (t < fadeDownDuration)
        {
            if (token.IsCancellationRequested) yield break;
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDownDuration);
            yield return null;
        }

        cg.alpha = 0f;
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

}

