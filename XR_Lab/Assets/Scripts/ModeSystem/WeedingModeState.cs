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

        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[WeedingModeState] PlantVisualRegistry is null.");
            return;
        }

        // All known plants are marked as protected ("don't touch").
        // The tint forces alpha to the selected color, making normally-transparent prefabs visible.
        context.PlantVisualRegistry.MarkAllProtected(protectedTint, disableTouchForProtectedPlants);
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
    }
}
