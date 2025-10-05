using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StoryVariables", menuName = "Scriptable Objects/StoryVariables")]
public class StoryVariables : ScriptableObject
{
    public List<StoryVar> rawData;

    public Dictionary<string, string> data;

    public void Init()
    {
        data = new Dictionary<string, string>();
        foreach (StoryVar vari in rawData)
        {
            data.Add(vari.id, vari.value);
        }
    }
}
