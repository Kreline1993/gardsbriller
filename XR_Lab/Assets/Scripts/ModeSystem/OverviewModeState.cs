using System.Collections.Generic;
using UnityEngine;

public sealed class OverviewModeState : ModeStateBase
{
    private static readonly Color LowMoistureColor = new Color(0.5f, 0f, 1f, 1f); // purple
    private static readonly Color BadHealthColor = new Color(1f, 0.5f, 0f, 1f); // orange
    private static readonly Color WarningTagColor = new Color(1f, 0f, 0f, 1f); // red

    private const int LowMoistureThreshold = 30;

    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[OverviewModeState] PlantVisualRegistry is null.");
            return;
        }

        Dictionary<string, Color> alertColors = BuildAlertColorMap();
        context.PlantVisualRegistry.ApplyPerPlantColors(alertColors, Color.white, false);
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
            map[plant.plantId] = LowMoistureColor;

        // Orange: bad health (overwrites purple)
        List<Plant> badHealthPlants = db.GetPlantsWhere(
            plant => plant.healthStatus == "bad");
        foreach (Plant plant in badHealthPlants)
            map[plant.plantId] = BadHealthColor;

        // Red: warning tag (highest priority, overwrites all)
        List<Plant> warningPlants = db.GetPlantsWhere(
            plant => plant.notes != null && plant.notes.noteTag == "warning");
        foreach (Plant plant in warningPlants)
            map[plant.plantId] = WarningTagColor;

        Debug.Log($"[OverviewModeState] Alert map built: {map.Count} plants flagged.");
        return map;
    }
}