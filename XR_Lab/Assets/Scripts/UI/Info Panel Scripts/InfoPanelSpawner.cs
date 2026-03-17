using UnityEngine;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;

    [Header("Auto Close")]
    [SerializeField, Min(0f)] private float closeDistanceFromPlant = 2.5f;

    private GameObject spawnedPanel;

    private PlantRuleOutlineController _outlineController;
    private Transform _viewerTransform;

    private void Awake()
    {
        _outlineController = GetComponent<PlantRuleOutlineController>();
    }

    private void Update()
    {
        if (spawnedPanel == null || closeDistanceFromPlant <= 0f)
            return;

        if (!TryGetViewerTransform(out Transform viewerTransform))
            return;

        float closeDistanceSqr = closeDistanceFromPlant * closeDistanceFromPlant;
        float distanceToPlantSqr = (viewerTransform.position - transform.position).sqrMagnitude;
        if (distanceToPlantSqr > closeDistanceSqr)
            ClosePanel();
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

            if (!TryGetViewerTransform(out Transform viewerTransform))
            {
                Debug.LogWarning("[InfoPanelSpawner] No viewer transform found. Ensure the scene has a MainCamera.");
                return;
            }

            // 2. Position logic
            Vector3 directionToPlayer = (viewerTransform.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + (directionToPlayer * 0.5f) + (Vector3.up * 0.5f);
            
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            spawnedPanel.transform.LookAt(viewerTransform);
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

    private bool TryGetViewerTransform(out Transform viewerTransform)
    {
        if (_viewerTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                _viewerTransform = mainCamera.transform;
        }

        viewerTransform = _viewerTransform;
        return viewerTransform != null;
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