using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PlantPickingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.PlantPicking;

    private readonly PlantHighlightState _highlightState = new PlantHighlightState();
    private List<Plant> _eligiblePlants = new List<Plant>();
    private readonly Dictionary<string, Color> _speciesTints;

    public PlantPickingModeState(ModeContext context, Dictionary<string, Color> speciesTints) : base(context)
    {
        _speciesTints = speciesTints;
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
        var selectedIds = _highlightState.SelectedIds;
        var plantTints = new Dictionary<string, Color>();

        foreach (var plant in _eligiblePlants)
        {
            if (!selectedIds.Contains(plant.plantId)) continue;

            Color tint = _speciesTints.TryGetValue(plant.species, out Color c) ? c : Color.white;
            plantTints[plant.plantId] = tint;
        }

        context.PlantVisualRegistry.ApplyProtectedSet(plantTints, false);
    }
}