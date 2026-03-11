using System.Collections.Generic;
using UnityEngine;

public sealed class PlantPickingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.PlantPicking;

    private readonly PlantHighlightState _highlightState = new PlantHighlightState();
    private List<Plant> _eligiblePlants = new List<Plant>();
    private readonly Color _highlightTint;

    public PlantPickingModeState(ModeContext context, Color highlightTint) : base(context)
    {
        _highlightTint = highlightTint;
    }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
        _highlightState.ClearAll();

        if (context.TwinDatabase == null)
        {
            Debug.LogWarning("[PlantPickingModeState] TwinDatabase is null.");
            return;
        }

        var all = context.TwinDatabase.GetPlantsWhere(_ => true);
        _eligiblePlants = PlantHighlightState.FilterEligible(all);
        _highlightState.SetEligiblePlants(_eligiblePlants);

        Debug.Log($"[PlantPickingModeState] {_eligiblePlants.Count} mature plants ready for highlighting.");
    }

    public override void Exit()
    {
        _highlightState.ClearAll();
        context.PlantVisualRegistry?.ResetAll();
    }

    public void ToggleSpecies(string species)
    {
        foreach (var plant in _eligiblePlants)
        {
            if (!string.Equals(plant.species, species, System.StringComparison.OrdinalIgnoreCase)) continue;

            if (_highlightState.IsSelected(plant.plantId))
                _highlightState.Deselect(plant.plantId);
            else
                _highlightState.Select(plant.plantId);
        }

        ApplyVisuals();
    }

    public void ClearAll()
    {
        _highlightState.ClearAll();
        context.PlantVisualRegistry?.ResetAll();
    }

    /// <summary>
    /// Returns true if at least one eligible plant of the given species is currently selected.
    /// </summary>
    public bool IsSpeciesSelected(string species)
    {
        foreach (var plant in _eligiblePlants)
        {
            if (string.Equals(plant.species, species, System.StringComparison.OrdinalIgnoreCase)
                && _highlightState.IsSelected(plant.plantId))
                return true;
        }
        return false;
    }

    private void ApplyVisuals()
    {
        if (context.PlantVisualRegistry == null) return;
        var selected = new HashSet<string>(_highlightState.SelectedIds);
        context.PlantVisualRegistry.ApplyProtectedSet(selected, _highlightTint, false);
    }
}