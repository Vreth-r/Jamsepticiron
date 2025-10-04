using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;
/*
public class TimedOptionsView : MonoBehaviour, OptionViewContainer
{
    [Header("Prefabs and UI")]
    public Button optionButtonPrefab; // Assign a prefab with Text n Button
    public Transform container; // Where to put the options
    public float[] optionDelays; // Seconds before each option appears

    private List<Button> activeOptions = new List<Button>();
    private OptionSet optionSet;
    private System.Action<int> onOptionSelected;

    public void AddOption(OptionSet.Option option)
    {
        // Not used — Yarn calls SetOptions instead
    }

    public void SetOptions(OptionSet options, System.Action<int> onOptionSelected)
    {
        this.optionSet = options;
        this.onOptionSelected = onOptionSelected;

        // Clear old
        foreach (var btn in activeOptions)
            Destroy(btn.gameObject);
        activeOptions.Clear();

        // Start timed reveal
        StartCoroutine(RevealOptions(options));
    }

    private IEnumerator RevealOptions(OptionSet options)
    {
        for (int i = 0; i < options.Options.Length; i++)
        {
            float delay = (i < optionDelays.Length) ? optionDelays[i] : 0f;
            yield return new WaitForSeconds(delay);

            var opt = options.Options[i];
            var btn = Instantiate(optionButtonPrefab, container);
            btn.GetComponentInChildren<TMPro.TMP_Text>().text = opt.Line.TextID; 
            int optionIndex = opt.ID;

            btn.onClick.AddListener(() =>
            {
                onOptionSelected?.Invoke(optionIndex);
                HideOptions();
            });

            activeOptions.Add(btn);
        }
    }

    public void HideOptions()
    {
        foreach (var btn in activeOptions)
            Destroy(btn.gameObject);
        activeOptions.Clear();
    }
}

*/