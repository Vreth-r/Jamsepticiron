using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CameraAnimDB", menuName = "DB/CameraAnimDB")]
public class CameraAnimDB : ScriptableObject
{
    public List<CameraAnimation> rawData;
    public Dictionary<string, CameraAnimation> data;

    public void Init()
    {
        data = new Dictionary<string, CameraAnimation>();
        foreach (CameraAnimation anim in rawData)
        {
            data.Add(anim.id, anim);
        }
    }
}