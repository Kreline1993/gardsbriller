using UnityEngine;

[RequireComponent(typeof(PlantIdentity))]
public class PlantHighlighter : MonoBehaviour, IPlantHighlighter
{
    [SerializeField] private Material highlightMaterial;

    private PlantIdentity _identity;
    private Renderer _renderer;
    private Material _originalMaterial;

    private void Awake()
    {
        _identity = GetComponent<PlantIdentity>();
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
            _originalMaterial = _renderer.material;
    }

    private void Start()
    {
        if (PlantHighlightController.Instance != null)
            PlantHighlightController.Instance.RegisterHighlighter(this);
        else
            Debug.LogWarning($"[PlantHighlighter] No PlantHighlightController found for {_identity.plantId}");
    }

    private void OnDestroy()
    {
        if (PlantHighlightController.Instance != null)
            PlantHighlightController.Instance.UnregisterHighlighter(this);
    }

    public void SetHighlight(string plantId, bool highlighted)
    {
        if (_identity.plantId != plantId) return;
        if (_renderer == null) return;

        _renderer.material = highlighted ? highlightMaterial : _originalMaterial;
    }

    public void ClearAllHighlights()
    {
        if (_renderer == null) return;
        _renderer.material = _originalMaterial;
    }
}