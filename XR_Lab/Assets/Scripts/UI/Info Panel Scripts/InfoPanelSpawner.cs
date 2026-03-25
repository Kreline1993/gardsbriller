using System.Collections.Generic;
using UnityEngine;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;

    [Header("Panel Limit")]
    [Tooltip("Maximum info panels allowed open at once. When exceeded, the oldest panel is closed. Set to 0 for unlimited.")]
    [SerializeField, Min(0)] private int maxOpenPanels = 3;

    [Header("Auto Close")]
    [SerializeField, Min(0f)] private float closeDistanceFromPlant = 2.5f;

    [Header("Panel Placement")]
    [SerializeField, Min(0f)] private float panelSideOffset = 0.35f;
    [SerializeField] private float panelHeightAbovePlantTop = 0.15f;
    [SerializeField] private Vector2 panelDistanceFromViewer = new Vector2(0.7f, 1.5f);
    [SerializeField] private Vector2 viewerVerticalBand = new Vector2(-0.2f, 0.2f);
    [SerializeField] private bool flipPanelForward = true;

    [Header("Overlap Avoidance")]
    [Tooltip("Approximate world-space width of an info panel. Used for horizontal overlap detection from the viewer's perspective. Set to 0 to disable.")]
    [SerializeField, Min(0f)] private float panelWidth = 0.35f;
    [Tooltip("Extra horizontal gap to leave between adjacent panels.")]
    [SerializeField, Min(0f)] private float panelGap = 0.05f;
    [Tooltip("Maximum nudge iterations when resolving cascading overlaps.")]
    [SerializeField, Min(1)] private int maxNudgeIterations = 6;

    [Header("Panel Tether")]
    [SerializeField] private bool enableTether = true;
    [SerializeField, Min(0f)] private float tetherWidth = 0.01f;
    [SerializeField] private Color tetherColor = Color.cyan;
    [SerializeField] private Material tetherMaterial;
    [SerializeField] private Vector3 tetherPanelOffsetLocal = new Vector3(0f, -0.12f, 0f);
    [SerializeField] private float tetherPlantTopOffset = 0f;
    [SerializeField, Min(2)] private int tetherSegments = 8;
    [SerializeField] private float tetherCurveHeight = 0.12f;

    private static readonly List<InfoPanelSpawner> _openPanels = new List<InfoPanelSpawner>();

    private GameObject spawnedPanel;
    private LineRenderer _tetherLine;
    private Material _tetherMaterialInstance;
    private readonly Vector3[] _panelWorldCorners = new Vector3[4];

    // Performance Caching
    private Gradient _tetherGradient;
    private bool _needsVisualUpdate = true;

    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int TintColorProperty = Shader.PropertyToID("_TintColor");
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

    private PlantRuleOutlineController _outlineController;
    private Transform _viewerTransform;

    private void Awake()
    {
        _outlineController = GetComponent<PlantRuleOutlineController>();
    }

    private void OnValidate()
    {
        // Clamp inspector values
        if (panelDistanceFromViewer.x < 0f) panelDistanceFromViewer.x = 0f;
        if (panelDistanceFromViewer.y < panelDistanceFromViewer.x) panelDistanceFromViewer.y = panelDistanceFromViewer.x;
        if (viewerVerticalBand.y < viewerVerticalBand.x) viewerVerticalBand.y = viewerVerticalBand.x;
        if (tetherWidth < 0f) tetherWidth = 0f;
        if (tetherSegments < 2) tetherSegments = 2;

        // Flag visual refresh
        _needsVisualUpdate = true;
    }

    private void Update()
    {
        if (spawnedPanel == null) return;

        UpdateTether();

        if (closeDistanceFromPlant <= 0f) return;
        if (!TryGetViewerTransform(out Transform viewerTransform)) return;

        float closeDistanceSqr = closeDistanceFromPlant * closeDistanceFromPlant;
        float distanceToPlantSqr = (viewerTransform.position - transform.position).sqrMagnitude;
        if (distanceToPlantSqr > closeDistanceSqr)
            ClosePanel();
    }

    public void TogglePanel()
    {
        if (spawnedPanel == null)
        {
            var identity = GetComponent<PlantIdentity>();
            if (identity == null) return;

            if (TwinDatabase.Instance == null) return;

            string id = identity.plantId;
            Plant data = TwinDatabase.Instance.GetPlantById(id);
            Row row   = TwinDatabase.Instance.GetRowForPlant(id);

            if (!TryGetViewerTransform(out Transform viewerTransform)) return;

            EnforcePanelLimit();

            Vector3 spawnPos = ComputePanelSpawnPosition(viewerTransform);
            spawnPos = ResolveOverlap(spawnPos, viewerTransform);
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            FacePanelTowardsViewer(spawnedPanel.transform, viewerTransform);

            InfoPanelBinder binder = spawnedPanel.GetComponent<InfoPanelBinder>();
            if (binder != null) binder.Initialize(this);
            
            _needsVisualUpdate = true;
            CreateTether();
            UpdateTether();

            _openPanels.Add(this);
            _outlineController?.SetPanelOpen(true);

            if (data != null)
            {
                if (binder != null) binder.Populate(data, row);
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
            _openPanels.Remove(this);
            DestroyTether();
            Destroy(spawnedPanel);
            spawnedPanel = null;
            _outlineController?.SetPanelOpen(false);
        }
    }

    private void EnforcePanelLimit()
    {
        if (maxOpenPanels <= 0) return;

        _openPanels.RemoveAll(s => s == null || s.spawnedPanel == null);

        while (_openPanels.Count >= maxOpenPanels && _openPanels.Count > 0)
            _openPanels[0].ClosePanel();
    }

    private Vector3 ResolveOverlap(Vector3 initialPos, Transform viewerTransform)
    {
        if (panelWidth <= 0f || _openPanels.Count == 0) return initialPos;

        Vector3 viewerRight = viewerTransform.right;
        viewerRight.y = 0f;
        viewerRight.Normalize();

        float clearance = panelWidth + panelGap;
        Vector3 pos = initialPos;

        for (int iter = 0; iter < maxNudgeIterations; iter++)
        {
            bool clean = true;

            foreach (var other in _openPanels)
            {
                if (other == null || other.spawnedPanel == null) continue;

                Vector3 diff = pos - other.spawnedPanel.transform.position;
                float hDist = Vector3.Dot(diff, viewerRight);

                if (Mathf.Abs(hDist) < clearance)
                {
                    float push = clearance - Mathf.Abs(hDist);
                    float dir = (hDist >= 0f) ? 1f : -1f;
                    pos += viewerRight * (dir * push);
                    clean = false;
                    break;
                }
            }

            if (clean) break;
        }

        return pos;
    }

    private void OnDestroy()
    {
        _openPanels.Remove(this);
    }

    private bool TryGetViewerTransform(out Transform viewerTransform)
    {
        if (_viewerTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null) _viewerTransform = mainCamera.transform;
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

        Vector3 toViewer = (viewerPosition - anchor).normalized;
        Vector3 spawnPos = anchor + (toViewer * panelSideOffset);

        spawnPos.y = Mathf.Clamp(spawnPos.y, viewerPosition.y + viewerVerticalBand.x, viewerPosition.y + viewerVerticalBand.y);

        Vector3 vToP = spawnPos - viewerPosition;
        float dist = Mathf.Clamp(vToP.magnitude, panelDistanceFromViewer.x, panelDistanceFromViewer.y);
        return viewerPosition + (vToP.normalized * dist);
    }

    private void FacePanelTowardsViewer(Transform panelTransform, Transform viewerTransform)
    {
        Vector3 lookDir = viewerTransform.position - panelTransform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) lookDir = Vector3.forward;
        panelTransform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        if (flipPanelForward) panelTransform.Rotate(0f, 180f, 0f, Space.Self);
    }

    private Bounds GetPlantBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
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
        if (!enableTether || spawnedPanel == null || _tetherLine != null) return;

        GameObject tetherObject = new GameObject("InfoPanelTether");
        tetherObject.transform.SetParent(spawnedPanel.transform, false);

        _tetherLine = tetherObject.AddComponent<LineRenderer>();
        _tetherLine.useWorldSpace = true;
        _tetherLine.positionCount = tetherSegments + 1;
        _tetherLine.numCornerVertices = 4;
        _tetherLine.numCapVertices = 4;

        if (tetherMaterial != null)
        {
            _tetherMaterialInstance = new Material(tetherMaterial);
            _tetherLine.material = _tetherMaterialInstance;
        }

        ApplyTetherVisuals();
    }

    private void UpdateTether()
    {
        if (!enableTether) { DestroyTether(); return; }
        if (_tetherLine == null) CreateTether();
        if (_tetherLine == null || spawnedPanel == null) return;

        if (_needsVisualUpdate)
        {
            ApplyTetherVisuals();
            _needsVisualUpdate = false;
        }

        int segmentCount = Mathf.Max(2, tetherSegments);
        if (_tetherLine.positionCount != segmentCount + 1) _tetherLine.positionCount = segmentCount + 1;

        Vector3 plantPoint = GetPlantAnchorPoint();
        Vector3 panelPoint = GetPanelBottomCenterPoint();
        Vector3 controlPoint = ((panelPoint + plantPoint) * 0.5f) + (Vector3.up * tetherCurveHeight);

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 p0p1 = Vector3.Lerp(panelPoint, controlPoint, t);
            Vector3 p1p2 = Vector3.Lerp(controlPoint, plantPoint, t);
            _tetherLine.SetPosition(i, Vector3.Lerp(p0p1, p1p2, t));
        }
    }

    private Vector3 GetPanelBottomCenterPoint()
    {
        RectTransform rt = spawnedPanel.GetComponentInChildren<RectTransform>();
        if (rt == null) return spawnedPanel.transform.TransformPoint(tetherPanelOffsetLocal);
        rt.GetWorldCorners(_panelWorldCorners);
        return (_panelWorldCorners[0] + _panelWorldCorners[3]) * 0.5f;
    }

    private void DestroyTether()
    {
        if (_tetherLine != null) Destroy(_tetherLine.gameObject);
        _tetherLine = null;
        if (_tetherMaterialInstance != null) Destroy(_tetherMaterialInstance);
        _tetherMaterialInstance = null;
    }

    private void ApplyTetherVisuals()
    {
        if (_tetherLine == null) return;

        _tetherLine.widthMultiplier = tetherWidth;

        if (_tetherGradient == null) _tetherGradient = new Gradient();
        _tetherGradient.SetKeys(
            new[] { new GradientColorKey(tetherColor, 0f), new GradientColorKey(tetherColor, 1f) },
            new[] { new GradientAlphaKey(tetherColor.a, 0f), new GradientAlphaKey(tetherColor.a, 1f) }
        );
        _tetherLine.colorGradient = _tetherGradient;

        if (_tetherMaterialInstance == null) return;

        // XR/Quest Built-in RP Transparency fix
        _tetherMaterialInstance.renderQueue = 3000; 
        
        SetMaterialColorIfPresent(_tetherMaterialInstance, ColorProperty, tetherColor);
        SetMaterialColorIfPresent(_tetherMaterialInstance, BaseColorProperty, tetherColor);
        SetMaterialColorIfPresent(_tetherMaterialInstance, TintColorProperty, tetherColor);

        if (_tetherMaterialInstance.HasProperty(EmissionColorProperty))
        {
            _tetherMaterialInstance.SetColor(EmissionColorProperty, tetherColor * tetherColor.a);
            _tetherMaterialInstance.EnableKeyword("_EMISSION");
        }
    }

    private static void SetMaterialColorIfPresent(Material material, int propertyId, Color color)
    {
        if (material != null && material.HasProperty(propertyId))
            material.SetColor(propertyId, color);
    }

    // --- STATIC UTILITIES FOR HIGHLIGHT CONTROLLERS ---

    public static void CloseAllPanels()
    {
        foreach (var spawner in Object.FindObjectsByType<InfoPanelSpawner>(FindObjectsSortMode.None))
            spawner.ClosePanel();
    }

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