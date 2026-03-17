using UnityEngine;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;

    [Header("Auto Close")]
    [SerializeField, Min(0f)] private float closeDistanceFromPlant = 2.5f;

    [Header("Panel Placement")]
    [SerializeField, Min(0f)] private float panelSideOffset = 0.35f;
    [SerializeField] private float panelHeightAbovePlantTop = 0.15f;
    [SerializeField] private Vector2 panelDistanceFromViewer = new Vector2(0.7f, 1.5f);
    [SerializeField] private Vector2 viewerVerticalBand = new Vector2(-0.2f, 0.2f);
    [SerializeField] private bool flipPanelForward = true;

    [Header("Panel Tether")]
    [SerializeField] private bool enableTether = true;
    [SerializeField, Min(0f)] private float tetherWidth = 0.01f;
    [SerializeField] private Color tetherColor = Color.cyan;
    [SerializeField] private Material tetherMaterial;
    [SerializeField] private Vector3 tetherPanelOffsetLocal = new Vector3(0f, -0.12f, 0f);
    [SerializeField] private float tetherPlantTopOffset = 0f;
    [SerializeField, Min(2)] private int tetherSegments = 8;
    [SerializeField] private float tetherCurveHeight = 0.12f;

    private GameObject spawnedPanel;
    private LineRenderer _tetherLine;

    private PlantRuleOutlineController _outlineController;
    private Transform _viewerTransform;

    private void Awake()
    {
        _outlineController = GetComponent<PlantRuleOutlineController>();
    }

    private void OnValidate()
    {
        if (panelDistanceFromViewer.x < 0f)
            panelDistanceFromViewer.x = 0f;

        if (panelDistanceFromViewer.y < panelDistanceFromViewer.x)
            panelDistanceFromViewer.y = panelDistanceFromViewer.x;

        if (viewerVerticalBand.y < viewerVerticalBand.x)
            viewerVerticalBand.y = viewerVerticalBand.x;

        if (tetherWidth < 0f)
            tetherWidth = 0f;

        if (tetherSegments < 2)
            tetherSegments = 2;
    }

    private void Update()
    {
        if (spawnedPanel == null)
            return;

        UpdateTether();

        if (closeDistanceFromPlant <= 0f)
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
            Vector3 spawnPos = ComputePanelSpawnPosition(viewerTransform);
            
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            FacePanelTowardsViewer(spawnedPanel.transform, viewerTransform);
            CreateTether();
            UpdateTether();

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
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (spawnedPanel != null)
        {
            DestroyTether();
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

    private Vector3 ComputePanelSpawnPosition(Transform viewerTransform)
    {
        Vector3 viewerPosition = viewerTransform.position;
        Bounds plantBounds = GetPlantBounds();

        Vector3 anchor = plantBounds.center;
        anchor.y = plantBounds.max.y + panelHeightAbovePlantTop;

        Vector3 toViewer = viewerPosition - anchor;
        if (toViewer.sqrMagnitude < 0.0001f)
            toViewer = viewerTransform.forward;
        toViewer.Normalize();

        Vector3 spawnPos = anchor + (toViewer * panelSideOffset);

        float minAllowedY = viewerPosition.y + viewerVerticalBand.x;
        float maxAllowedY = viewerPosition.y + viewerVerticalBand.y;
        spawnPos.y = Mathf.Clamp(spawnPos.y, minAllowedY, maxAllowedY);

        Vector3 viewerToPanel = spawnPos - viewerPosition;
        float viewerDistance = viewerToPanel.magnitude;
        if (viewerDistance < 0.0001f)
        {
            viewerToPanel = viewerTransform.forward;
            viewerDistance = panelDistanceFromViewer.x;
        }
        else
        {
            viewerToPanel /= viewerDistance;
        }

        float clampedDistance = Mathf.Clamp(viewerDistance, panelDistanceFromViewer.x, panelDistanceFromViewer.y);
        return viewerPosition + (viewerToPanel * clampedDistance);
    }

    private void FacePanelTowardsViewer(Transform panelTransform, Transform viewerTransform)
    {
        Vector3 lookDirection = viewerTransform.position - panelTransform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = viewerTransform.forward;
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude < 0.0001f)
            lookDirection = Vector3.forward;

        panelTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        if (flipPanelForward)
            panelTransform.Rotate(0f, 180f, 0f, Space.Self);
    }

    private Bounds GetPlantBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders != null && colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            return bounds;
        }

        return new Bounds(transform.position, Vector3.zero);
    }

    private Vector3 GetPlantAnchorPoint()
    {
        Bounds bounds = GetPlantBounds();
        return new Vector3(bounds.center.x, bounds.max.y + tetherPlantTopOffset, bounds.center.z);
    }

    private void CreateTether()
    {
        if (!enableTether || spawnedPanel == null || _tetherLine != null)
            return;

        GameObject tetherObject = new GameObject("InfoPanelTether");
        tetherObject.transform.SetParent(spawnedPanel.transform, false);

        _tetherLine = tetherObject.AddComponent<LineRenderer>();
        _tetherLine.useWorldSpace = true;
        _tetherLine.positionCount = tetherSegments + 1;
        _tetherLine.widthMultiplier = tetherWidth;
        _tetherLine.startColor = tetherColor;
        _tetherLine.endColor = tetherColor;
        _tetherLine.numCornerVertices = 4;
        _tetherLine.numCapVertices = 4;

        if (tetherMaterial != null)
            _tetherLine.material = tetherMaterial;
    }

    private void UpdateTether()
    {
        if (!enableTether)
        {
            DestroyTether();
            return;
        }

        if (_tetherLine == null)
            CreateTether();

        if (_tetherLine == null || spawnedPanel == null)
            return;

        int segmentCount = Mathf.Max(2, tetherSegments);
        if (_tetherLine.positionCount != segmentCount + 1)
            _tetherLine.positionCount = segmentCount + 1;

        _tetherLine.widthMultiplier = tetherWidth;
        _tetherLine.startColor = tetherColor;
        _tetherLine.endColor = tetherColor;

        Vector3 panelPoint = spawnedPanel.transform.TransformPoint(tetherPanelOffsetLocal);
        Vector3 plantPoint = GetPlantAnchorPoint();
        Vector3 controlPoint = ((panelPoint + plantPoint) * 0.5f) + (Vector3.up * tetherCurveHeight);

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 p0p1 = Vector3.Lerp(panelPoint, controlPoint, t);
            Vector3 p1p2 = Vector3.Lerp(controlPoint, plantPoint, t);
            Vector3 curvedPoint = Vector3.Lerp(p0p1, p1p2, t);
            _tetherLine.SetPosition(i, curvedPoint);
        }
    }

    private void DestroyTether()
    {
        if (_tetherLine == null)
            return;

        Destroy(_tetherLine.gameObject);
        _tetherLine = null;
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