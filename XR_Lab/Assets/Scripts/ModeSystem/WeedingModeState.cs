using UnityEngine;
using System.Collections.Generic;

public sealed class WeedingModeState : ModeStateBase
{
    private readonly Color protectedTint;
    private readonly bool disableTouchForProtectedPlants;
    private readonly GameObject overlayPrefab;
    private readonly bool hideOriginalDuringOverlay;
    private readonly PickingProximityController proximityController;
    private readonly float nearHighlightDistance;

    public override AppMode Mode => AppMode.Weeding;

    public WeedingModeState(
        ModeContext context,
        Color protectedTint,
        bool disableTouchForProtectedPlants,
        PickingProximityController proximityController,
        float nearHighlightDistance,
        GameObject overlayPrefab = null,
        bool hideOriginalDuringOverlay = true)
        : base(context)
    {
        this.protectedTint = protectedTint;
        this.disableTouchForProtectedPlants = disableTouchForProtectedPlants;
        this.proximityController = proximityController;
        this.nearHighlightDistance = nearHighlightDistance;
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

        context.PlantVisualRegistry.ResetAll();

        if (proximityController != null)
        {
            Camera cam = Camera.main;
            if (cam == null)
                Debug.LogWarning("[WeedingModeState] Camera.main is null - proximity highlights will not update.");

            proximityController.InitialiseWeeding(
                cam?.transform,
                context.PlantVisualRegistry,
                nearHighlightDistance,
                protectedTint,
                disableTouchForProtectedPlants);

            List<Plant> allPlants = context.TwinDatabase != null
                ? context.TwinDatabase.GetPlantsWhere(_ => true)
                : new List<Plant>();

            proximityController.SetSelectedPlants(allPlants);

            foreach (var infoPanel in Object.FindObjectsByType<InfoPanelSpawner>(FindObjectsSortMode.None))
                infoPanel.ClosePanel();
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
        proximityController?.ClearPlants();
        context.PlantVisualRegistry?.ResetAll();
    }
}
