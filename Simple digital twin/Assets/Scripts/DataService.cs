using UnityEngine;
using System.IO;
using FarmSystem.Models;


public class DataService
{
    public static Plant GetPlantById(string id) {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "PlantData.json");
        
        if (!System.IO.File.Exists(path)) {
            Debug.LogError("JSON file not found at " + path);
            return null;
        }

        string jsonString = System.IO.File.ReadAllText(path);
        FarmData data = JsonUtility.FromJson<FarmData>(jsonString);

        if (data == null || data.rows == null) return null;

        // Nested search: Loop through rows, then plants
        foreach (Row row in data.rows) {
            foreach (Plant p in row.plants) {
                if (p.plantId == id) return p;
            }
        }
        return null;
    }
}