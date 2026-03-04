using System.Collections.Generic;
using UnityEngine;

public sealed class OverviewModeState : ModeStateBase
{
    private readonly Color lowMoistureColor;
    private readonly Color badHealthColor;
    private readonly Color warningTagColor;
    private readonly GameObject overlayPrefab;
    private readonly bool hideOriginalDuringOverlay;

    private const int LowMoistureThreshold = 30;

    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(
        ModeContext context,
        Color lowMoistureColor,
        Color badHealthColor,
        Color warningTagColor,
        GameObject overlayPrefab = null,
        bool hideOriginalDuringOverlay = true)
        : base(context)
    {
        this.lowMoistureColor = lowMoistureColor;
        this.badHealthColor = badHealthColor;
        this.warningTagColor = warningTagColor;
        this.overlayPrefab = overlayPrefab;
        this.hideOriginalDuringOverlay = hideOriginalDuringOverlay;
    }

    public override void Enter()
    {
        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[OverviewModeState] PlantVisualRegistry is null.");
            return;
        }

        Dictionary<string, Color> alertColors = BuildAlertColorMap();

        if (overlayPrefab != null)
        {
            context.PlantVisualRegistry.ApplyAlertOverlaysOnly(
                overlayPrefab, alertColors, false, hideOriginalDuringOverlay);
        }
        else
        {
            context.PlantVisualRegistry.ApplyPerPlantColors(alertColors, Color.white, false);
        }
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    private Dictionary<string, Color> BuildAlertColorMap()
    {
        var map = new Dictionary<string, Color>();

        TwinDatabase db = context.TwinDatabase;
        if (db == null)
        {
            Debug.LogWarning("[OverviewModeState] TwinDatabase not found – skipping alert colouring.");
            return map;
        }

        // Purple: row groundMoisture below threshold (lowest priority)
        List<Plant> lowMoisturePlants = db.GetPlantsWhere(
            (plant, row) => row.groundMoisture < LowMoistureThreshold);
        foreach (Plant plant in lowMoisturePlants)
            map[plant.plantId] = lowMoistureColor;

        // Orange: bad health (overwrites purple)
        List<Plant> badHealthPlants = db.GetPlantsWhere(
            plant => plant.healthStatus == "bad");
        foreach (Plant plant in badHealthPlants)
            map[plant.plantId] = badHealthColor;

        // Red: warning tag (highest priority, overwrites all)
        List<Plant> warningPlants = db.GetPlantsWhere(
            plant => plant.notes != null && plant.notes.noteTag == "warning");
        foreach (Plant plant in warningPlants)
            map[plant.plantId] = warningTagColor;

        Debug.Log($"[OverviewModeState] Alert map built: {map.Count} plants flagged.");
        return map;
    }
}