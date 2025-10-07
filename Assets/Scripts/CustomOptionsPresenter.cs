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

    private const string TruncateLastLineMarkupName = "lastline";

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

        foreach (var view in optionViews)
        {
            _ = HandleOptionLifecycleAsync(view, cancellationToken); // fire and forget each option
        }

        // allow interactivity and wait for an option to be selected
        /*
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        */

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

    private async Task HandleOptionLifecycleAsync(OptionItem view, CancellationToken cancellationToken)
    {
        try
        {
            float delay = ParseValue<float>(view.Option.Line.Metadata[0]);
            float duration = ParseValue<float>(view.Option.Line.Metadata[1]);
            string vPos = ParseValue<string>(view.Option.Line.Metadata[2]);
            string hPos = ParseValue<string>(view.Option.Line.Metadata[3]);

            // Wait before showing
            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

            // Position setup
            RectTransform rect = view.gameObject.GetComponent<RectTransform>();
            Vector2 anchoredPos = Vector2.zero;

            // Basic position mapping – adjust as needed
            switch (vPos.ToLower())
            {
                case "top": anchoredPos.y = 85f; break;
                case "middle": anchoredPos.y = 65f; break;
                case "bottom": anchoredPos.y = 55f; break;
            }
            switch (hPos.ToLower())
            {
                case "left": anchoredPos.x = -25f; break;
                case "center": anchoredPos.x = 0f; break;
                case "right": anchoredPos.x = 45f; break;
            }

            rect.anchoredPosition = anchoredPos;

            // Fade in
            CanvasGroup group = view.gameObject.GetComponent<CanvasGroup>();
            await Effects.FadeAlphaAsync(group, 0f, 1f, fadeUpDuration, cancellationToken);

            // Wait for duration or cancellation
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(duration), cancellationToken);
            }
            catch (TaskCanceledException) { /* option skipped early */ }

            // Fade out
            await Effects.FadeAlphaAsync(group, 1f, 0f, fadeDownDuration, cancellationToken);

            // Optional: disable raycast after fade-out
            group.blocksRaycasts = false;
            group.interactable = false;
        }
        catch (TaskCanceledException)
        {
            // Cancel gracefully — no exceptions in console spam
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error handling dialogue option: {ex}");
        }
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

