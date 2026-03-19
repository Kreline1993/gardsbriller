using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanelSpawner : MonoBehaviour
{
    public GameObject infoPanelPrefab;
    public GameObject notePanelPrefab;

    [Header("Auto Close")]
    [SerializeField, Min(0f)] private float closeDistanceFromPlant = 2.5f;

    [Header("Panel Placement")]
    [SerializeField, Min(0f)] private float panelSideOffset = 0.35f;
    [SerializeField] private float panelHeightAbovePlantTop = 0.15f;
    [SerializeField] private Vector2 panelDistanceFromViewer = new Vector2(0.7f, 1.5f);
    [SerializeField] private Vector2 viewerVerticalBand = new Vector2(-0.2f, 0.2f);
    [SerializeField] private bool flipPanelForward = true;

    [Header("Note Panel Placement")]
    [SerializeField] private float notePanelOffsetFromInfoPanel = 0.5f;
    [SerializeField] private float notePanelForwardOffset = 0.3f;

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
    private GameObject spawnedNotePanel;
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
        if (spawnedPanel == null && spawnedNotePanel == null) return;

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

            Vector3 spawnPos = ComputePanelSpawnPosition(viewerTransform);
            spawnedPanel = Instantiate(infoPanelPrefab, spawnPos, Quaternion.identity);
            FacePanelTowardsViewer(spawnedPanel.transform, viewerTransform);
            
            _needsVisualUpdate = true;
            CreateTether();
            UpdateTether();

            _outlineController?.SetPanelOpen(true);

            if (data != null)
            {
                InfoPanelBinder binder = spawnedPanel.GetComponent<InfoPanelBinder>();
                if (binder != null) binder.Populate(data, row);
            }

            // Spawn note panel if plant has notes
            if (data != null && HasNotes(data) && notePanelPrefab != null)
            {
                Vector3 noteSpawnPos = ComputeNotePanelSpawnPosition(spawnedPanel.transform, viewerTransform);
                spawnedNotePanel = Instantiate(notePanelPrefab, noteSpawnPos, Quaternion.identity);
                FacePanelTowardsViewer(spawnedNotePanel.transform, viewerTransform);

                InfoPanelBinder noteBinder = spawnedNotePanel.GetComponent<InfoPanelBinder>();
                if (noteBinder != null) noteBinder.PopulateNote(data);

                // Wire up close button
                WireNoteCloseButton();
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

        if (spawnedNotePanel != null)
        {
            Destroy(spawnedNotePanel);
            spawnedNotePanel = null;
        }
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

    private bool HasNotes(Plant plant)
    {
        if (plant?.notes == null) return false;
        string text = plant.notes.textNote;
        string tag = plant.notes.noteTag;
        return !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(tag);
    }

    private Vector3 ComputeNotePanelSpawnPosition(Transform infoPanelTransform, Transform viewerTransform)
    {
        // Position the note panel to the right of the info panel and forward towards the viewer
        Vector3 infoPanelPos = infoPanelTransform.position;
        Vector3 toViewer = (viewerTransform.position - infoPanelPos).normalized;
        
        // Get the right direction relative to the viewer
        Vector3 right = Vector3.Cross(Vector3.up, toViewer).normalized;
        
        // Offset to the right (closer to info panel) and forward towards viewer
        return infoPanelPos + right * notePanelOffsetFromInfoPanel + toViewer * notePanelForwardOffset;
    }

    private void WireNoteCloseButton()
    {
        if (spawnedNotePanel == null) return;

        // Find all buttons on the note panel
        Button[] buttons = spawnedNotePanel.GetComponentsInChildren<Button>();
        
        foreach (Button btn in buttons)
        {
            // Check if this button has a text component with "Close"
            TMPro.TextMeshProUGUI tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null && tmpText.text.Equals("Close", System.StringComparison.OrdinalIgnoreCase))
            {
                // Wire the close button to close the note panel
                btn.onClick.AddListener(() => { if (spawnedNotePanel != null) Destroy(spawnedNotePanel); spawnedNotePanel = null; });
                break;
            }
        }
    }
}