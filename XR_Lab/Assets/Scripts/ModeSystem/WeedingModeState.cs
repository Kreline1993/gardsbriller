using System.Collections.Generic;
using UnityEngine;

public sealed class WeedingModeState : ModeStateBase
{
    private readonly Color protectedTint;
    private readonly bool disableTouchForProtectedPlants;

    public override AppMode Mode => AppMode.Weeding;

    public WeedingModeState(ModeContext context, Color protectedTint, bool disableTouchForProtectedPlants)
        : base(context)
    {
        this.protectedTint = protectedTint;
        this.disableTouchForProtectedPlants = disableTouchForProtectedPlants;
    }

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
            if (plant == null || string.IsNullOrEmpty(plant.plantId))
                continue;

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
            protectedTint,
            disableTouchForProtectedPlants
        );
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
    }
}
