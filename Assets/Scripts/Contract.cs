using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class Contract : MonoBehaviour
{
    [Header("Positions")]
    public Vector3 tablePos;
    public Vector3 tableRot;

    public Vector3 viewPos;
    public Vector3 viewRot;

    private bool isMoving = false;

    public TMP_InputField nameInput;
    private bool isNameFilled = false;

    private Queue<IEnumerator> moveQueue = new Queue<IEnumerator>();

    public void Start()
    {
        Transform caret = (((gameObject.transform.Find("Canvas")).Find("NameInput")).Find("Text Area")).Find("Caret");
        Destroy(caret.gameObject); // fuck this thing and its whole family.
    }

    public void Update()
    {
        // If idle and there’s a queued move, start it
        if (!isMoving && moveQueue.Count > 0)
        {
            StartCoroutine(RunNext());
        }
    }

    private IEnumerator RunNext()
    {
        isMoving = true;
        yield return StartCoroutine(moveQueue.Dequeue());
        isMoving = false;
    }

    public void MoveToView()
    {
        if (!isNameFilled)
        {
            nameInput.Select();
            nameInput.ActivateInputField();
        }
        moveQueue.Enqueue(MoveRoutine(viewPos, viewRot, 1));
    }

    public void MoveToTable()
    {
        moveQueue.Enqueue(MoveRoutine(tablePos, tableRot, 1));
    }

    public void NameFilled()
    {
        GameManager.Instance.playerName = nameInput.text;
        isNameFilled = true;
    }

    // i know im code duping just give me a damn minute here
    private IEnumerator MoveRoutine(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        Vector3 startPos = gameObject.transform.position;
        Quaternion startRot = gameObject.transform.rotation;

        Quaternion endRot = Quaternion.Euler(targetRot);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // smooth step curve
            float smoothT = t * t * (3f - 2f * t);

            gameObject.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            gameObject.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        // ensure exact final values because fuck why would it just go to where i said to go
        gameObject.transform.position = targetPos;
        gameObject.transform.rotation = endRot;
    }
}
