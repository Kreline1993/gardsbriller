using UnityEngine;
using TMPro;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;
    private GameObject spawnedPanel;

    public void TogglePanel()
    {
        if (spawnedPanel == null)
        {
            // 1) Identify plant
            var identity = GetComponent<PlantIdentity>();
            if (identity == null)
            {
                Debug.LogError("[InfoPanelSpa>wner] Missing PlantIdentity component on this plant prefab.");
                return;
            }

            if (TwinDatabase.Instance == null)
            {
                Debug.LogError("[InfoPanelSpawner] TwinDatabase.Instance is null. Add TwinDatabase to a Services GameObject in the scene.");
                return;
            }

            string id = identity.plantId;
            Plant data = TwinDatabase.Instance.GetPlantById(id);

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