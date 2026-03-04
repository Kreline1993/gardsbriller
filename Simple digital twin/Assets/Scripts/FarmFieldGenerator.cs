using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO; // Delete when removing pc only read

public class FarmFieldGenerator : MonoBehaviour
{
    public GameObject interactionPrefab;
    public float scaleFactor = 10.0f;

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
        if (FarmData == null || FarmData.rows == null) return;

        foreach (Row row in FarmData.rows)
        {
            // We still iterate through rows to get the plants, 
            // but we ignore row.location for the plant placement.
            foreach (Plant p in row.plants)
            {
                // Calculate position relative ONLY to world 0,0,0 plus scale
                Vector3 worldPos = new Vector3(
                    p.position.x * scaleFactor,
                    p.position.y * scaleFactor,
                    p.position.z * scaleFactor
                );

                GameObject ghostPlant = Instantiate(interactionPrefab, worldPos, Quaternion.identity, this.transform);

                PlantIdentity identity = ghostPlant.GetComponent<PlantIdentity>();
                if (identity != null)
                {
                    identity.plantId = p.plantId;
                }
                ghostPlant.name = $"Trigger_{p.species}_{p.plantId}";
            }
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        string path = Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        if (!File.Exists(path)) return;

        try
        {
            string jsonString = File.ReadAllText(path);
            FarmData data = JsonUtility.FromJson<FarmData>(jsonString);

            if (data == null || data.rows == null) return;

            foreach (Row row in data.rows)
            {
                // 1. Position calculation
                Vector3 rowBase = transform.position + new Vector3(row.location.x, row.location.y, row.location.z) * scaleFactor;

                // 2. Dimensions - Added a fallback to ensure size isn't 0
                float w = (row.size != null ? row.size.width : 1f) * scaleFactor;
                float l = (row.size != null ? row.size.length : 1f) * scaleFactor;
                float h = 0.2f;

                // 3. Center calculation (Unity draws from center, JSON usually provides corner)
                Vector3 rowCenter = rowBase + new Vector3(w / 2f, h / 2f, l / 2f);

                // --- DRAWING ---
                // Draw Row Outline
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(rowCenter, new Vector3(w, h, l));

                // Draw semi-transparent floor so it's easier to see
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawCube(rowCenter, new Vector3(w, h, l));

                // Draw Plants
                if (row.plants != null)
                {
                    Gizmos.color = Color.green;
                    foreach (Plant p in row.plants)
                    {
                        // Match the ParseAndGenerate logic: ignore rowBase
                        Vector3 pPos = new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                        Gizmos.DrawWireSphere(pPos, 0.1f * scaleFactor);
                    }
                }
            }
        }
        catch (System.Exception) { /* Fail silently */ }
    }
}
