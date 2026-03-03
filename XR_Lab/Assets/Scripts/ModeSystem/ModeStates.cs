using System.Collections.Generic;

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

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}

public sealed class OverviewModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}

public sealed class PlantPickingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.PlantPicking;

    public PlantPickingModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}

public sealed class WeedingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Weeding;

    public WeedingModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        UnityEngine.Debug.Log("[WeedingModeState] Entering Weeding mode.");
        
        if (context.TwinDatabase == null)
        {
            UnityEngine.Debug.LogWarning("[WeedingModeState] TwinDatabase is null.");
            return;
        }

        List<Plant> allPlants = context.TwinDatabase.GetPlantsWhere(plant => plant != null);
        UnityEngine.Debug.Log($"[WeedingModeState] Found {allPlants.Count} plants in database.");
        
        HashSet<string> protectedIds = new HashSet<string>();
        foreach (Plant plant in allPlants)
        {
            if (plant == null || string.IsNullOrEmpty(plant.plantId))
                continue;

            protectedIds.Add(plant.plantId);
        }
        
        UnityEngine.Debug.Log($"[WeedingModeState] Marking {protectedIds.Count} plants as protected.");

        if (context.PlantVisualRegistry == null)
        {
            UnityEngine.Debug.LogWarning("[WeedingModeState] PlantVisualRegistry is null.");
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
    public UnityEngine.Color WeedingProtectedTint { get; }
    public bool DisableTouchForProtectedPlants { get; }

    public ModeContext(
        TwinDatabase twinDatabase,
        PlantVisualRegistry plantVisualRegistry,
        UnityEngine.Color weedingProtectedTint,
        bool disableTouchForProtectedPlants)
    {
        TwinDatabase = twinDatabase;
        PlantVisualRegistry = plantVisualRegistry;
        WeedingProtectedTint = weedingProtectedTint;
        DisableTouchForProtectedPlants = disableTouchForProtectedPlants;
    }
}
