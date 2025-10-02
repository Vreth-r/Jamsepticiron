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
        camMoveQueue.Enqueue(MoveRoutine(targetPos, targetRot, duration));
    }

    private IEnumerator MoveRoutine(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        Vector3 startPos = mainCam.gameObject.transform.position;
        Quaternion startRot = mainCam.gameObject.transform.rotation;

        Quaternion endRot = Quaternion.Euler(targetRot);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // smooth step curve
            float smoothT = t * t * (3f - 2f * t);

            mainCam.gameObject.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            mainCam.gameObject.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        // ensure exact final values because fuck why would it just go to where i said to go
        mainCam.gameObject.transform.position = targetPos;
        mainCam.gameObject.transform.rotation = endRot;
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
        yield return new WaitForSeconds(seconds);
    }
}
