using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class SFXEntry
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Sound Effects")]
    public List<SFXEntry> soundEffects = new List<SFXEntry>();
    private Dictionary<string, AudioClip> sfxLookup = new Dictionary<string, AudioClip>();

    private AudioSource oneShotSource;
    private Dictionary<string, AudioSource> loopingSources = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;

        foreach (var entry in soundEffects)
        {
            if (!sfxLookup.ContainsKey(entry.name))
                sfxLookup.Add(entry.name, entry.clip);
        }
    }

    public void PlaySFX(string name)
    {
        if (sfxLookup.TryGetValue(name, out var clip))
        {
            oneShotSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX '{name}' not found!");
        }
    }

    public void PlayLoop(string name, float volume = 1f)
    {
        if (!sfxLookup.TryGetValue(name, out var clip))
        {
            Debug.LogWarning($"[AudioManager] Loop '{name}' not found!");
            return;
        }

        if (loopingSources.ContainsKey(name))
            return; // already playing

        var loopObj = new GameObject($"Loop_{name}");
        loopObj.transform.parent = transform;

        var source = loopObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.Play();

        loopingSources[name] = source;
    }

    public void StopLoop(string name)
    {
        if (!loopingSources.TryGetValue(name, out var src))
            return;

        src.Stop();
        Destroy(src.gameObject);
        loopingSources.Remove(name);
    }

    public void StopAllLoops()
    {
        foreach (var kvp in loopingSources)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        loopingSources.Clear();
    }

    public AudioClip GetClip(string name)
    {
        if (sfxLookup.TryGetValue(name, out var clip))
        {
            return clip;
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX '{name}' not found!");
        }
        return null;
    }
}

