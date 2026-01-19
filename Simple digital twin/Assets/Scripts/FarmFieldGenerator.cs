using UnityEngine;
using FarmSystem.Models;

public class FarmFieldGenerator : MonoBehaviour
{
    public GameObject interactionPrefab; 
    public float scaleFactor = 1.0f;

    void Start()
    {
        GenerateField();
    }

    void OnDrawGizmos()
    {
        // This allows you to see the positions in the editor even when not playing
        if (Application.isPlaying) return; 

        // Re-calculating logic just for visualization in Editor
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        if (!System.IO.File.Exists(path)) return;

        string jsonString = System.IO.File.ReadAllText(path);
        FarmData data = JsonUtility.FromJson<FarmData>(jsonString);

        if (data == null) return;

        Gizmos.color = Color.green;
        foreach (Row row in data.rows)
        {
            Vector3 rowBase = transform.position + new Vector3(row.location.x, 0, row.location.z) * scaleFactor;
            foreach (Plant p in row.plants)
            {
                Vector3 pPos = rowBase + new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                Gizmos.DrawWireSphere(pPos, 0.2f); // Draws a small sphere at each plant
            }
        }
    }

    void GenerateField()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        
        if (!System.IO.File.Exists(path)) {
            Debug.LogError($"[FarmGenerator] JSON not found at: {path}");
            return;
        }

        string jsonString = System.IO.File.ReadAllText(path);
        FarmData data = JsonUtility.FromJson<FarmData>(jsonString);

        if (data == null || data.rows == null) {
            Debug.LogError("[FarmGenerator] Failed to parse JSON. Check your structure!");
            return;
        }

        foreach (Row row in data.rows)
        {
            // Row position in world space
            Vector3 rowBasePos = new Vector3(row.location.x, 0, row.location.z) * scaleFactor;

            foreach (Plant p in row.plants)
            {
                Vector3 localPos = new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                Vector3 worldPos = rowBasePos + localPos;

                GameObject ghostPlant = Instantiate(interactionPrefab, worldPos, Quaternion.identity, this.transform);
                
                // Assign ID
                PlantIdentity identity = ghostPlant.GetComponent<PlantIdentity>();
                if (identity != null) {
                    identity.plantId = p.plantId;
                }
                
                ghostPlant.name = $"Trigger_{p.species}_{p.plantId}";
            }
        }
        Debug.Log("[FarmGenerator] Field generation complete.");
    }
}