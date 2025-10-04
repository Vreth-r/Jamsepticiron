/*
BY: Michael Latka
*/
using UnityEngine;
using Yarn.Unity;
using TMPro;
using System.Collections;
using System.Collections.Generic;


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

    [Header("Script Hooks")]
    public CameraController camScript;

    [Header("Gloabls")]
    public string playerName;

    // Camera Movement vars
    private Queue<IEnumerator> camMoveQueue = new Queue<IEnumerator>();
    private bool isCamMoving = false;

    void Awake()
    {
        Instance = this;
        cameraAnims.Init();
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
    public void MoveCamera(string animName, int duration)
    {
        MoveTo(cameraAnims.data[animName].targetCoords, cameraAnims.data[animName].targetRotation, duration);
    }

    [YarnCommand("Wait")]
    public static void WaitCommand(float seconds)
    {
        Instance.camMoveQueue.Enqueue(Instance.WaitRoutine(seconds));
    }

    private IEnumerator WaitRoutine(float seconds)
    {
        camScript.enableControl = false;
        yield return new WaitForSeconds(seconds);
        camScript.enableControl = true;
    }
}
