using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO; // Delete when removing pc only read

public class FarmFieldGenerator : MonoBehaviour
{
    public GameObject interactionPrefab;
    public float scaleFactor = 1.0f;

    public FarmData FarmData { get; private set; }

    void Start()
    {
        StartCoroutine(GenerateFieldRoutine());
    }

    IEnumerator GenerateFieldRoutine()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        string jsonString = "";

        // Check if using Android/Quest
        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[FarmGenerator] Error loading JSON from APK: {webRequest.error}");
                    yield break;
                }
                jsonString = webRequest.downloadHandler.text;
            }
        }
        else
        {
            // Standard PC/Editor loading - Delete when testing finished
            if (!File.Exists(path))
            {
                Debug.LogError($"[FarmGenerator] JSON not found at: {path}");
                yield break;
            }
            jsonString = File.ReadAllText(path);
        }

        ParseAndGenerate(jsonString);
    }

    void ParseAndGenerate(string jsonString)
    {
        FarmData = JsonUtility.FromJson<FarmData>(jsonString);
        FarmData data = FarmData;

        if (data == null || data.rows == null)
        {
            Debug.LogError("[FarmGenerator] Failed to parse JSON. Check your structure!");
            return;
        }

        foreach (Row row in data.rows)
        {
            Vector3 rowBasePos = new Vector3(row.location.x, row.location.y, row.location.z) * scaleFactor;

            foreach (Plant p in row.plants)
            {
                Vector3 localPos = new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                Vector3 worldPos = transform.position + rowBasePos + localPos;

                GameObject ghostPlant = Instantiate(interactionPrefab, worldPos, Quaternion.identity, this.transform);

                PlantIdentity identity = ghostPlant.GetComponent<PlantIdentity>();
                if (identity != null)
                {
                    identity.plantId = p.plantId;
                }

                ghostPlant.name = $"Trigger_{p.species}_{p.plantId}";
            }
        }
        Debug.Log("[FarmGenerator] Field generation complete.");
    }

    void OnDrawGizmos()
    {
        // Draw Gizmos in live editior to see plants -- Delete after testing
        if (Application.isPlaying) return;

        string path = Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        if (!File.Exists(path)) return;

        string jsonString = File.ReadAllText(path);
        FarmData data = JsonUtility.FromJson<FarmData>(jsonString);
        if (data == null) return;

        Gizmos.color = Color.green;
        foreach (Row row in data.rows)
        {
            Vector3 rowBase = transform.position + new Vector3(row.location.x, row.location.y, row.location.z) * scaleFactor;
            foreach (Plant p in row.plants)
            {
                Vector3 pPos = rowBase + new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                Gizmos.DrawWireSphere(pPos, 0.2f);
            }
        }
    }
}