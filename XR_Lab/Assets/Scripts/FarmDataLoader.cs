using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;

public class FarmDataLoader : MonoBehaviour
{
    [SerializeField] private string fileName = "PlantData.json";

    public IEnumerator LoadFarmDataRoutine(Action<FarmData> onLoaded)
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
                    Debug.LogError($"[FarmDataLoader] Error loading JSON from APK: {webRequest.error}");
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
                Debug.LogError($"[FarmDataLoader] JSON not found at: {path}");
                onLoaded?.Invoke(null);
                yield break;
            }

            jsonString = File.ReadAllText(path);
        }

        onLoaded?.Invoke(Parse(jsonString));
    }

    public bool TryLoadFarmDataEditor(out FarmData data)
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

    private FarmData Parse(string jsonString)
    {
        FarmData data = JsonUtility.FromJson<FarmData>(jsonString);
        if (data == null || data.rows == null)
            return null;

        return data;
    }
}