/*
BY: Michael Latka
*/
using UnityEngine;
using Yarn.Unity;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.SceneManagement;


// for simplicity this script will also function as the yarn commander
// its monolithic i DO NOT WANT TO HEAR IT I DO NOT CARE
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fonts and Text")]
    public TMP_FontAsset deathFont;
    public TMP_FontAsset playerFont;
    public LinePresenter linePresenter;
    public DialogueRunner dr;

    [Header("Data Hooks")]
    public CameraAnimDB cameraAnims;

    [Header("Object Hooks")]
    public Camera mainCam;
    public GameObject dialogueOptionPrefab;
    public Transform dialogueOptionParent;
    public GameObject spotLight;
    private Light spotLightComp;
    public GameObject skull;

    [Header("Script Hooks")]
    public CameraController camScript;

    [Header("Globals")]
    public string playerName;
    public Dictionary<string, int> endingVars;
    private List<DialogueOption> activeOptions = new();
    private bool optionSelected = false;
    private Coroutine pauseRoutine;

    // Camera Movement vars
    private Queue<IEnumerator> camMoveQueue = new Queue<IEnumerator>();
    private bool isCamMoving = false;

    void Awake()
    {
        Instance = this;
        cameraAnims.Init();
        camScript.enableControl = false;
        endingVars = new Dictionary<string, int>();
        endingVars.Add("Apathy", 0);
        endingVars.Add("Truth", 0);
        endingVars.Add("Killer", 0);
        spotLightComp = spotLight.GetComponent<Light>();
    }

    void Start()
    {
        dr.StartDialogue("StartNode");
    }

    void Update()
    {
        // If idle and there’s a queued move, start it
        if (!isCamMoving && camMoveQueue.Count > 0)
        {
            StartCoroutine(RunNext());
        }
    }

    //*********** CAMERA ************\\
    // I do it through this gateway method to keep the coroutine enumerator reference private.
    public void MoveTo(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        camMoveQueue.Enqueue(camScript.MoveRoutine(targetPos, targetRot, duration));
    }

    private IEnumerator RunNext()
    {
        isCamMoving = true;
        yield return StartCoroutine(camMoveQueue.Dequeue());
        isCamMoving = false;
    }

    //*********** YARN ***************\\

    [YarnCommand("ChangeFont")]
    public void ChangeFont(string fontName)
    {
        if (fontName == "death")
        {
            linePresenter.lineText.font = deathFont;
        }
        else if (fontName == "player")
        {
            linePresenter.lineText.font = playerFont;
        }
    }

    [YarnCommand("MoveCamera")]
    public void MoveCamera(string animName, float duration)
    {
        MoveTo(cameraAnims.data[animName].targetCoords, cameraAnims.data[animName].targetRotation, duration);
    }

    [YarnCommand("WaitCamera")]
    public static void WaitCamera(float seconds)
    {
        Instance.camMoveQueue.Enqueue(Instance.WaitRoutine(seconds));
    }

    [YarnCommand("Wait")]
    public static void Wait(string nextNode, float duration)
    {
        // Stop the current dialogue entirely
        GameManager.Instance.dr.Stop();

        // Start coroutine to wait before resuming
        if (GameManager.Instance.pauseRoutine == null)
        {
            GameManager.Instance.pauseRoutine = GameManager.Instance.StartCoroutine(GameManager.Instance.ResumeAfterDelay(nextNode, duration));
        }
    }

    private IEnumerator ResumeAfterDelay(string nodeName, float delay)
    {
        //Debug.Log($"Stoppin Yarn for {delay} seconds before jumping to '{nodeName}'...");
        yield return new WaitForSeconds(delay);

        pauseRoutine = null;
        // Resume at target node
        dr.StartDialogue(nodeName);
    }

    [YarnCommand("DialogueOption")]
    public void SpawnOption(string displayText, string nextNode, float delay, float duration, string vertLocation, string horizontalLocation, string endingVarTarget = "Truth", int value = 0)
    {
        float x = 0;
        float y = 0;
        float z = 0;
        if (vertLocation == "top") { y = 85; }

        if (vertLocation == "bot") { y = 55; }

        if (vertLocation == "mid") { y = 65; }

        if (horizontalLocation == "L") { x = -25; }

        if (horizontalLocation == "R") { x = 45; }
        Vector3 spawnPos = new Vector3(x, y, z);
        var obj = Instantiate(dialogueOptionPrefab, spawnPos, Quaternion.identity, dialogueOptionParent);
        var option = obj.GetComponent<DialogueOption>();
        option.Setup(displayText, nextNode, delay, duration);
        endingVars[endingVarTarget] += value;
        activeOptions.Add(option);
    }

    [YarnCommand("WaitForOptions")]
    public IEnumerator WaitForOptions()
    {
        // Wait until an option is chosen or all options fade out
        optionSelected = false;
        while (!optionSelected && activeOptions.Count > 0)
            yield return null;
    }

    public void OnOptionClicked(string nextNode)
    {
        optionSelected = true;

        // Fade out all others
        foreach (var opt in activeOptions)
            if (opt != null)
                opt.FadeOutAndDestroy();

        activeOptions.Clear();
        if (pauseRoutine != null)
        {
            StopCoroutine(pauseRoutine);
            pauseRoutine = null;
            //Debug.Log("[PauseAndJump] Coroutine canceled.");
        }
        dr.StartDialogue(nextNode);
    }

    [YarnCommand("DialogueOptionSpecific")]
    public void SpawnOptionSpecific(string displayText, string nextNode, float delay, float duration, float x, float y, float z, string endingVarTarget = "Truth", int value = 0)
    {
        Vector3 spawnPos = new Vector3(x, y, z);
        var opt = Instantiate(dialogueOptionPrefab, spawnPos, Quaternion.identity, dialogueOptionParent);
        DialogueOption script = opt.GetComponent<DialogueOption>();
        script.Setup(displayText, nextNode, delay, duration);
        endingVars[endingVarTarget] += value;
    }

    [YarnCommand("CameraInput")]
    public void CameraInput(bool value)
    {
        camScript.enableControl = value;
    }

    [YarnCommand("BranchApathy")]
    public void BranchApathy()
    {
        if (endingVars["Apathy"] == 5)
        {
            dr.StartDialogue("apathyEnding");
        }
        else if (endingVars["Apathy"] <= 1)
        {
            dr.StartDialogue("rememberThem");
        }
        else
        {
            dr.StartDialogue("mediocrePerformanceEnding");
        }
    }

    [YarnCommand("BranchKillerTruth")]
    public void BranchKillerTruth(string displayText1, string nextNode1, float delay1, float duration1, string vertLocation1, string horizontalLocation1,
        string displayText2, string nextNode2, float delay2, float duration2, string vertLocation2, string horizontalLocation2)
    {
        if (endingVars["Killer"] >= endingVars["Truth"])
        {
            SpawnOption(displayText1, nextNode1, delay1, duration1, vertLocation1, horizontalLocation1);
        }
        if (endingVars["Truth"] >= endingVars["Killer"])
        {
            SpawnOption(displayText2, nextNode2, delay2, duration2, vertLocation2, horizontalLocation2);
        }
    }

    [YarnCommand("SetAdvanceDelay")]
    public void SetAdvanceDelay(float delay)
    {
        linePresenter.autoAdvanceDelay = delay;
    }

    //************** WORLD YARN *******************\\
    [YarnCommand("LightMode")]
    public void LightMode(bool status)
    {
        spotLightComp.enabled = status;
        Debug.Log(status);
    }

    [YarnCommand("SkullToggle")]
    public void SkullToggle(bool toggle)
    {
        skull.SetActive(toggle);
    }

    [YarnCommand("FadeToScene")]
    public void FadeToScene(string sceneName)
    {
        SceneTransitionManager.Instance?.FadeToScene(sceneName);
    }

    [YarnCommand("FadeOut")]
    public void FadeOut(float duration = 1f)
    {
        SceneTransitionManager.Instance?.FadeOut(duration);
    }

    [YarnCommand("FadeIn")]
    public void FadeIn(float duration = 1f)
    {
        SceneTransitionManager.Instance?.FadeIn(duration);
    }

    //************** ANIMATION YARN ****************\\
    [YarnCommand("PlayAnim")]
    public void PlayAnimation(string objectName, string animName, bool loop = false, float duration = -1f)
    {
        AnimationManager.Instance?.PlayAnimation(objectName, animName, loop, duration);
    }

    [YarnCommand("StopAnim")]
    public void StopAnimation(string objectName, string animName)
    {
        AnimationManager.Instance?.StopAnimation(objectName, animName);
    }
    //************** AUDIO YARN ********************\\
    [YarnCommand("PlaySFX")]
    public void PlaySFX(string sfxName)
    {
        AudioManager.Instance?.PlaySFX(sfxName);
    }

    [YarnCommand("StopAllSFX")]
    public void StopAllSFX()
    {
        var src = AudioManager.Instance?.GetComponent<AudioSource>();
        if (src != null)
            src.Stop();
    }

    [YarnCommand("PlaySFXAt")]
    public void PlaySFXAt(string sfxName, string targetName)
    {
        GameObject target = GameObject.Find(targetName);
        if (target == null)
        {
            Debug.LogWarning($"[PlaySFXAt] Could not find GameObject '{targetName}'.");
            return;
        }

        if (AudioManager.Instance == null) return;
        var clip = AudioManager.Instance.GetClip(sfxName);
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, target.transform.position);
    }

    [YarnCommand("PlayLoop")]
    public void PlayLoop(string sfxName, float volume = 1f)
    {
        AudioManager.Instance?.PlayLoop(sfxName, volume);
    }

    [YarnCommand("StopLoop")]
    public void StopLoop(string sfxName)
    {
        AudioManager.Instance?.StopLoop(sfxName);
    }

    [YarnCommand("StopAllLoops")]
    public void StopAllLoops()
    {
        AudioManager.Instance?.StopAllLoops();
    }

    private IEnumerator WaitRoutine(float seconds)
    {
        camScript.enableControl = false;
        yield return new WaitForSeconds(seconds);
        camScript.enableControl = true;
    }
}
