using UnityEngine;
using TMPro;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;
    private GameObject spawnedPanel;

    private PlantRuleOutlineController _outlineController;

    private void Awake()
    {
        _outlineController = GetComponent<PlantRuleOutlineController>();
    }

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
            Row row   = TwinDatabase.Instance.GetRowForPlant(id);

            // 2. Position logic
            Vector3 directionToPlayer = (Camera.main.transform.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (directionToPlayer * 0.5f) + (Vector3.up * 0.5f);
            
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            spawnedPanel.transform.LookAt(Camera.main.transform);
            spawnedPanel.transform.Rotate(0, 180, 0);

            _outlineController?.SetPanelOpen(true);

            // 3. Update the UI Text
            if (data != null)
            {
                InfoPanelBinder binder = spawnedPanel.GetComponent<InfoPanelBinder>();
                if (binder != null)
                    binder.Populate(data, row);
                else
                    Debug.LogWarning("[InfoPanelSpawner] InfoPanelBinder not found on panel prefab.");
            }
            else
            {
                Debug.LogWarning("[InfoPanelSpawner] No plant data found for ID: " + id);
            }
        }
        else
        {
            Destroy(spawnedPanel);
            spawnedPanel = null;
            _outlineController?.SetPanelOpen(false);
        }
    }

    public void ClosePanel()
    {
        if (spawnedPanel != null)
        {
            Destroy(spawnedPanel);
            spawnedPanel = null;
            _outlineController?.SetPanelOpen(false);
        }
    }

    /// <summary>
    /// Closes all info panels across the scene. Call when entering a mode where plants are uninteractable.
    /// </summary>
    public static void CloseAllPanels()
    {
        foreach (var spawner in Object.FindObjectsByType<InfoPanelSpawner>(FindObjectsSortMode.None))
            spawner.ClosePanel();
    }

    /// <summary>
    /// Closes info panels only for plants that are NOT in the highlighted set.
    /// Use when the interaction filter restricts interaction to highlighted plants – close orphaned
    /// panels on plants that became uninteractable, but keep panels open on highlighted plants.
    /// </summary>
    public static void ClosePanelsForNonHighlighted(System.Collections.Generic.HashSet<string> highlightedPlantIds)
    {
        if (highlightedPlantIds == null || highlightedPlantIds.Count == 0) return;

        foreach (var spawner in Object.FindObjectsByType<InfoPanelSpawner>(FindObjectsSortMode.None))
        {
            var identity = spawner.GetComponent<PlantIdentity>();
            if (identity == null || string.IsNullOrEmpty(identity.plantId)) continue;
            if (highlightedPlantIds.Contains(identity.plantId)) continue;
            spawner.ClosePanel();
        }
    }
}