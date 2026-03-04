using System.Collections.Generic;
using UnityEngine;

public abstract class ModeStateBase : IModeState
{
    protected readonly ModeContext context;

    public abstract AppMode Mode { get; }

    protected ModeStateBase(ModeContext context)
    {
        this.context = context;
    }

    public abstract void Enter();
    public abstract void Exit();
    public virtual void Tick() { }
}

public sealed class DefaultModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Default;

    public DefaultModeState(ModeContext context) : base(context) { }

    public override void Enter() { context.PlantVisualRegistry?.ResetAll(); }
    public override void Exit() { }
}

public sealed class OverviewModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(ModeContext context) : base(context) { }

    public override void Enter() { context.PlantVisualRegistry?.ResetAll(); }
    public override void Exit() { }
}

public sealed class PlantPickingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.PlantPicking;

    private readonly PlantHighlightState _highlightState = new PlantHighlightState();
    private List<Plant> _eligiblePlants = new List<Plant>();

    public PlantPickingModeState(ModeContext context) : base(context) { }

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

    /// <summary>
    /// Toggles the highlight for all mature plants of the given species.
    /// Calling again with the same species will turn them off.
    /// </summary>
    public void ToggleSpecies(string species)
    {
        foreach (var plant in _eligiblePlants)
        {
            if (plant.species != species) continue;

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

    private void ApplyVisuals()
    {
        if (context.PlantVisualRegistry == null) return;
        var selected = new HashSet<string>(_highlightState.SelectedIds);
        context.PlantVisualRegistry.ApplyProtectedSet(selected, context.PickingHighlightTint, false);
    }
}

public sealed class WeedingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Weeding;

    public WeedingModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        Debug.Log("[WeedingModeState] Entering Weeding mode.");

        if (context.TwinDatabase == null)
        {
            Debug.LogWarning("[WeedingModeState] TwinDatabase is null.");
            return;
        }

        List<Plant> allPlants = context.TwinDatabase.GetPlantsWhere(plant => plant != null);
        Debug.Log($"[WeedingModeState] Found {allPlants.Count} plants in database.");

        HashSet<string> protectedIds = new HashSet<string>();
        foreach (Plant plant in allPlants)
        {
            if (plant == null || string.IsNullOrEmpty(plant.plantId)) continue;
            protectedIds.Add(plant.plantId);
        }

        Debug.Log($"[WeedingModeState] Marking {protectedIds.Count} plants as protected.");

        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[WeedingModeState] PlantVisualRegistry is null.");
            return;
        }

        context.PlantVisualRegistry.ApplyProtectedSet(
            protectedIds,
            context.WeedingProtectedTint,
            context.DisableTouchForProtectedPlants
        );
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
    }
}

public sealed class ModeContext
{
    public TwinDatabase TwinDatabase { get; }
    public PlantVisualRegistry PlantVisualRegistry { get; }
    public Color WeedingProtectedTint { get; }
    public bool DisableTouchForProtectedPlants { get; }
    public Color PickingHighlightTint { get; }

    public ModeContext(
        TwinDatabase twinDatabase,
        PlantVisualRegistry plantVisualRegistry,
        Color weedingProtectedTint,
        bool disableTouchForProtectedPlants,
        Color pickingHighlightTint)
    {
        TwinDatabase = twinDatabase;
        PlantVisualRegistry = plantVisualRegistry;
        WeedingProtectedTint = weedingProtectedTint;
        DisableTouchForProtectedPlants = disableTouchForProtectedPlants;
        PickingHighlightTint = pickingHighlightTint;
    }
}