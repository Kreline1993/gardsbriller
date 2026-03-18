using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

public class TwinDataLoader : MonoBehaviour
{
    [SerializeField] private string fileName = "PlantData.json";

    public IEnumerator LoadTwinDataRoutine(Action<TwinData> onLoaded)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        string jsonString;

        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[TwinDataLoader] Error loading JSON from APK: {webRequest.error}");
                    onLoaded?.Invoke(null);
                    yield break;
                }

                jsonString = webRequest.downloadHandler.text;
            }
        }
        else
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[TwinDataLoader] JSON not found at: {path}");
                onLoaded?.Invoke(null);
                yield break;
            }

            jsonString = File.ReadAllText(path);
        }

        onLoaded?.Invoke(Parse(jsonString));
    }

    public bool TryLoadTwinDataEditor(out TwinData data)
    {
        data = null;
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
            return false;

        try
        {
            data = Parse(File.ReadAllText(path));
            return data != null;
        }
        catch
        {
            return false;
        }
    }

    private TwinData Parse(string jsonString)
    {
        // Remove "notes": null so JsonUtility leaves the field as null.
        // Unity's JsonUtility cannot represent null for [Serializable] custom types and may
        // instantiate an empty object instead, which can cause plants without notes to be
        // incorrectly treated as having notes (orange info panel header).
        //
        // Notes in middle: ', "notes": null, ' -> ', '
        jsonString = Regex.Replace(jsonString, @",\s*""notes""\s*:\s*null\s*,", ", ");
        // Notes as last field: ', "notes": null }' -> ' }' (remove trailing comma from prev field)
        jsonString = Regex.Replace(jsonString, @",\s*""notes""\s*:\s*null\s*([\s\r\n]*})", "$1");
        // Notes as first field: '{ "notes": null, "plantId": ... }' -> '{ "plantId": ... }'
        jsonString = Regex.Replace(jsonString, @"{\s*""notes""\s*:\s*null\s*,", "{");
        // Notes as only field: '{ "notes": null }' -> '{}' (empty object)
        jsonString = Regex.Replace(jsonString, @"{\s*""notes""\s*:\s*null\s*}", "{}");

        TwinData data = JsonUtility.FromJson<TwinData>(jsonString);
        if (data == null || data.rows == null)
            return null;

        return data;
    }
}