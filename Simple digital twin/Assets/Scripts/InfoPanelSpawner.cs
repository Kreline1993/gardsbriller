using UnityEngine;
using TMPro;
using FarmSystem.Models;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;
    private GameObject spawnedPanel;

    public void TogglePanel()
    {
        if (spawnedPanel == null)
        {
            // 1. Identify which plant this is
            string id = GetComponent<PlantIdentity>().plantId;
            Plant data = DataService.GetPlantById(id);

            // 2. Position logic
            Vector3 directionToPlayer = (Camera.main.transform.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (directionToPlayer * 0.5f) + (Vector3.up * 0.5f);
            
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            spawnedPanel.transform.LookAt(Camera.main.transform);
            spawnedPanel.transform.Rotate(0, 180, 0);

            // 3. Update the UI Text
            if (data != null) {
                TMP_Text text = spawnedPanel.GetComponentInChildren<TMP_Text>();
                text.text = $"<b>{data.species}</b>\nID: {data.plantId}";
            }
            else {
                Debug.LogWarning("No plant data found for ID: " + id);
            }
        }
        else
        {
            Destroy(spawnedPanel);
        }
    }
}