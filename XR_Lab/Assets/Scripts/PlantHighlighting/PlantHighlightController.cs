using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Orchestrates plant highlighting. Attach to Twin Manager (or a child GameObject).
///
/// Responsibilities:
///   - Waits for TwinDatabase to load, then queries plants with growth >= 100
///   - Delegates selection logic to PlantHighlightState
///   - Notifies registered IPlantHighlighter components to update visuals
///   - Fires events that PlantSelectionMenu (and any other UI) subscribes to
///
/// How to connect visual highlighters:
///   In your IPlantHighlighter implementation's Start(), call:
///       PlantHighlightController.Instance.RegisterHighlighter(this);
///   In its OnDestroy(), call:
///       PlantHighlightController.Instance?.UnregisterHighlighter(this);
/// </summary>
public class PlantHighlightController : MonoBehaviour
{
    public static PlantHighlightController Instance { get; private set; }

    [SerializeField] private TwinDatabase twinDatabase;
    [Tooltip("How long to wait for TwinDatabase to populate before giving up.")]
    [SerializeField] private float dataTimeoutSeconds = 5f;

    private readonly PlantHighlightState _state = new PlantHighlightState();
    private readonly List<Plant> _eligiblePlants = new List<Plant>();
    private readonly List<IPlantHighlighter> _highlighters = new List<IPlantHighlighter>();

    // -- Events for menu / UI to subscribe to ----------

    /// <summary>Fired once the eligible plant list is ready. Arg: read-only list of eligible plants.</summary>
    public event Action<IReadOnlyList<Plant>> OnEligiblePlantsReady;

    /// <summary>Fired when a single plant's highlight state changes. Args: (plantId, isHighlighted)</summary>
    public event Action<string, bool> OnHighlightChanged;

    /// <summary>Fired when all highlights are cleared at once.</summary>
    public event Action OnAllHighlightsCleared;

    // -- Public read access ----------

    public IReadOnlyList<Plant> EligiblePlants => _eligiblePlants;

    // -- Unity lifecycle ----------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (twinDatabase == null)
            twinDatabase = TwinDatabase.Instance;
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(WaitForData());
        RefreshEligiblePlants();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator WaitForData()
    {
        float elapsed = 0f;
        while (elapsed < dataTimeoutSeconds)
        {
            if (twinDatabase != null)
            {
                var rows = twinDatabase.GetRowsWhere(_ => true);
                if (rows.Count > 0) yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Debug.LogWarning("[PlantHighlightController] Timed out waiting for plant data. " +
                         "Eligible plant list may be empty.");
    }

    // -- Public API ----------

    /// <summary>
    /// Re-queries TwinDatabase for all plants with growth >= 100 and rebuilds the eligible list.
    /// Call this if the underlying data changes at runtime.
    /// </summary>
    public void RefreshEligiblePlants()
    {
        if (twinDatabase == null)
        {
            Debug.LogWarning("[PlantHighlightController] No TwinDatabase reference.");
            return;
        }

        var all = twinDatabase.GetPlantsWhere(_ => true);
        var eligible = PlantHighlightState.FilterEligible(all);

        _eligiblePlants.Clear();
        _eligiblePlants.AddRange(eligible);
        _state.SetEligiblePlants(eligible);

        OnEligiblePlantsReady?.Invoke(_eligiblePlants);
    }

    /// <summary>
    /// Set the highlight state of one plant.
    /// Has no effect if the plant's growth is below 100.
    /// </summary>
    public void SetPlantHighlight(string plantId, bool highlighted)
    {
        bool changed = highlighted ? _state.Select(plantId) : _state.Deselect(plantId);
        if (!changed) return;

        ApplyHighlight(plantId, highlighted);
        OnHighlightChanged?.Invoke(plantId, highlighted);
    }

    /// <summary>Highlight all eligible plants.</summary>
    public void HighlightAll()
    {
        _state.SelectAll();
        foreach (var plant in _eligiblePlants)
        {
            ApplyHighlight(plant.plantId, true);
            OnHighlightChanged?.Invoke(plant.plantId, true);
        }
    }

    /// <summary>Remove all highlights.</summary>
    public void ClearAllHighlights()
    {
        _state.ClearAll();
        for (int i = _highlighters.Count - 1; i >= 0; i--)
        {
            if (_highlighters[i] == null) { _highlighters.RemoveAt(i); continue; }
            _highlighters[i].ClearAllHighlights();
        }
        OnAllHighlightsCleared?.Invoke();
    }

    public bool IsHighlighted(string plantId) => _state.IsSelected(plantId);
    public bool IsEligible(string plantId) => _state.IsEligible(plantId);

    // --- Highlighter registration ----------

    /// <summary>Call from your IPlantHighlighter component's Start() to register it.</summary>
    public void RegisterHighlighter(IPlantHighlighter highlighter)
    {
        if (highlighter != null && !_highlighters.Contains(highlighter))
            _highlighters.Add(highlighter);
    }

    /// <summary>Call from your IPlantHighlighter component's OnDestroy() to unregister.</summary>
    public void UnregisterHighlighter(IPlantHighlighter highlighter)
    {
        _highlighters.Remove(highlighter);
    }

    // --- Internal ----------

    private void ApplyHighlight(string plantId, bool highlighted)
    {
        for (int i = _highlighters.Count - 1; i >= 0; i--)
        {
            if (_highlighters[i] == null) { _highlighters.RemoveAt(i); continue; }
            _highlighters[i].SetHighlight(plantId, highlighted);
        }
    }
}
