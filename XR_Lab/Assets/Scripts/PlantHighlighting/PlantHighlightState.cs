using System;
using System.Collections.Generic;

/// <summary>
/// Manages which plants are eligible (growth >= 100) and which are currently selected.
/// Kept separate so it can be fully tested in EditMode without a running scene.
/// </summary>
public class PlantHighlightState
{
    public const int GrowthThreshold = 100;

    private readonly HashSet<string> _eligibleIds = new HashSet<string>();
    private readonly HashSet<string> _selectedIds = new HashSet<string>();

    public IReadOnlyCollection<string> SelectedIds => _selectedIds;
    public IReadOnlyCollection<string> EligibleIds => _eligibleIds;

    /// <summary>Fired when a single plant's selection state changes. Args: (plantId, isSelected)</summary>
    public event Action<string, bool> OnSelectionChanged;

    /// <summary>Fired when ClearAll() is called.</summary>
    public event Action OnSelectionCleared;

    // --- Filtering ----------

    /// <summary>
    /// Returns only the plants with growth >= GrowthThreshold.
    /// Static so it can be called without an instance (useful in tests and utilities).
    /// </summary>
    public static List<Plant> FilterEligible(IEnumerable<Plant> plants)
    {
        var result = new List<Plant>();
        foreach (var plant in plants)
            if (plant != null && plant.growth >= GrowthThreshold)
                result.Add(plant);
        return result;
    }

    // --- Eligible plant management ----------

    /// <summary>
    /// Rebuilds the eligible set from the given plant list.
    /// Resets all current selections — call this when data refreshes.
    /// </summary>
    public void SetEligiblePlants(IEnumerable<Plant> plants)
    {
        _eligibleIds.Clear();
        _selectedIds.Clear();

        foreach (var plant in plants)
        {
            if (plant == null || string.IsNullOrEmpty(plant.plantId)) continue;
            if (plant.growth >= GrowthThreshold)
                _eligibleIds.Add(plant.plantId);
        }
    }

    public bool IsEligible(string plantId) => !string.IsNullOrEmpty(plantId) && _eligibleIds.Contains(plantId);

    // --- Selection management ----------

    /// <summary>
    /// Selects a plant. Returns true if the state changed.
    /// Does nothing and returns false if the plant is not eligible.
    /// </summary>
    public bool Select(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return false;
        if (!_eligibleIds.Contains(plantId)) return false;
        if (!_selectedIds.Add(plantId)) return false; // Already selected

        OnSelectionChanged?.Invoke(plantId, true);
        return true;
    }

    /// <summary>
    /// Deselects a plant. Returns true if the state changed.
    /// </summary>
    public bool Deselect(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return false;
        if (!_selectedIds.Remove(plantId)) return false; // Was not selected

        OnSelectionChanged?.Invoke(plantId, false);
        return true;
    }

    /// <summary>Selects all eligible plants.</summary>
    public void SelectAll()
    {
        foreach (var id in _eligibleIds)
        {
            if (_selectedIds.Add(id))
                OnSelectionChanged?.Invoke(id, true);
        }
    }

    /// <summary>Clears all selections and fires OnSelectionCleared.</summary>
    public void ClearAll()
    {
        _selectedIds.Clear();
        OnSelectionCleared?.Invoke();
    }

    public bool IsSelected(string plantId) => !string.IsNullOrEmpty(plantId) && _selectedIds.Contains(plantId);
}