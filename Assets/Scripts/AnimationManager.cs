using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance;

    [System.Serializable]
    public class AnimEntry
    {
        public string objectName;
        public Animator animator;
    }

    public List<AnimEntry> animatorList;
    private Dictionary<string, Animator> animators = new Dictionary<string, Animator>();
    private Dictionary<string, Coroutine> runningCoroutines = new Dictionary<string, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
        foreach (var entry in animatorList)
        {
            if (!animators.ContainsKey(entry.objectName))
                animators.Add(entry.objectName, entry.animator);
        }
    }

    // Register animators by name later i think i need this
    public void RegisterAnimator(string key, Animator animator)
    {
        if (!animators.ContainsKey(key))
            animators.Add(key, animator);
    }

    /// <summary>Play an animation immediately. Optionally loop it.</summary>
    public void PlayAnimation(string key, string animName, bool loop = false, float duration = -1f)
    {
        if (!animators.TryGetValue(key, out var animator))
        {
            Debug.LogWarning($"Animator '{key}' not found!");
            return;
        }

        // Stop any currently running auto-stop coroutine for this object
        if (runningCoroutines.ContainsKey(key))
        {
            StopCoroutine(runningCoroutines[key]);
            runningCoroutines.Remove(key);
        }

        // Handle loop parameter: assumes you have a bool parameter in the Animator called "<AnimName>_Loop"
        animator.SetBool(animName + "_Loop", loop);

        animator.Play(animName);

        // Auto-stop after duration (if specified)
        if (duration > 0f)
        {
            Coroutine co = StartCoroutine(AutoStopAnimation(key, animName, duration, loop));
            runningCoroutines[key] = co;
        }
    }

    /// <summary>Stop a specific animation loop manually.</summary>
    public void StopAnimation(string key, string animName)
    {
        if (!animators.TryGetValue(key, out var animator))
            return;

        // Stop any running coroutine
        if (runningCoroutines.ContainsKey(key))
        {
            StopCoroutine(runningCoroutines[key]);
            runningCoroutines.Remove(key);
        }

        animator.SetBool(animName + "_Loop", false);
        animator.Play("Idle"); // default idle
    }

    private IEnumerator AutoStopAnimation(string key, string animName, float duration, bool loop)
    {
        yield return new WaitForSeconds(duration);

        if (animators.TryGetValue(key, out var animator))
        {
            if (loop)
                animator.SetBool(animName + "_Loop", false);

            animator.Play("Idle"); // default idle
        }

        runningCoroutines.Remove(key);
    }
}
