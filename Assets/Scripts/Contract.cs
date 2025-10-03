using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Contract : MonoBehaviour
{
    [Header("Positions")]
    public Vector3 tablePos;
    public Vector3 tableRot;

    public Vector3 viewPos;
    public Vector3 viewRot;

    private bool isMoving = false;

    private Queue<IEnumerator> moveQueue = new Queue<IEnumerator>();

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
        Debug.Log("move to view");
        moveQueue.Enqueue(MoveRoutine(viewPos, viewRot, 1));
    }

    public void MoveToTable()
    {
        Debug.Log("move to table");
        moveQueue.Enqueue(MoveRoutine(tablePos, tableRot, 1));
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
