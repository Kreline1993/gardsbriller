using UnityEngine;

public sealed class WeedingModeState : ModeStateBase
{
    private readonly Color protectedTint;
    private readonly bool disableTouchForProtectedPlants;
    private readonly GameObject overlayPrefab;
    private readonly bool hideOriginalDuringOverlay;

    public override AppMode Mode => AppMode.Weeding;

    public WeedingModeState(
        ModeContext context,
        Color protectedTint,
        bool disableTouchForProtectedPlants,
        GameObject overlayPrefab = null,
        bool hideOriginalDuringOverlay = true)
        : base(context)
    {
        this.protectedTint = protectedTint;
        this.disableTouchForProtectedPlants = disableTouchForProtectedPlants;
        this.overlayPrefab = overlayPrefab;
        this.hideOriginalDuringOverlay = hideOriginalDuringOverlay;
    }

    public override void Enter()
    {
        Debug.Log("[WeedingModeState] Entering Weeding mode.");

        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[WeedingModeState] PlantVisualRegistry is null.");
            return;
        }

        if (overlayPrefab != null)
        {
            context.PlantVisualRegistry.MarkAllProtectedWithOverlay(
                overlayPrefab, protectedTint, disableTouchForProtectedPlants, hideOriginalDuringOverlay);
        }
        else
        {
            context.PlantVisualRegistry.MarkAllProtected(
                protectedTint, disableTouchForProtectedPlants);
        }

        foreach (var infoPanel in Object.FindObjectsByType<InfoPanelSpawner>(FindObjectsSortMode.None))
            infoPanel.ClosePanel();
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
    }
}
