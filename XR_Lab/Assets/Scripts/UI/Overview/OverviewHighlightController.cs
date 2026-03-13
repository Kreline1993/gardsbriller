using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies color tints to plants/rows when overview panel section buttons are expanded.
/// Works like picking mode highlights - uses PlantVisualRegistry.ApplyProtectedSet.
/// Call RefreshHighlights from OverviewPanelBinder when section expand state changes.
/// </summary>
public class OverviewHighlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlantVisualRegistry plantVisualRegistry;
    [SerializeField] private TwinDatabase twinDatabase;
    [SerializeField] private ModeController modeController;

    [Header("Colors")]
    [SerializeField] private Color lowMoistureColor = new Color(0.5f, 0f, 1f, 1f);
    [SerializeField] private Color badHealthColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color warningTagColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private Color ripeColor = new Color(0f, 0.8f, 0.2f, 1f);

    private void Awake()
    {
        if (plantVisualRegistry == null) plantVisualRegistry = GetComponent<PlantVisualRegistry>();
        if (plantVisualRegistry == null) plantVisualRegistry = FindFirstObjectByType<PlantVisualRegistry>();
        if (twinDatabase == null) twinDatabase = FindFirstObjectByType<TwinDatabase>();
        if (modeController == null) modeController = FindFirstObjectByType<ModeController>();
    }

    /// <summary>
    /// Refreshes highlights based on which sections are expanded.
    /// Call this from OverviewPanelBinder when any section is toggled.
    /// </summary>
    public void RefreshHighlights(
        bool expandedLowMoisture,
        bool expandedBadHealth,
        bool expandedWarnings,
        bool expandedRipe,
        OverviewPanelDataSnapshot snapshot)
    {
        if (plantVisualRegistry == null) return;
        if (modeController != null && modeController.CurrentMode != AppMode.Overview)
        {
            plantVisualRegistry.ResetAll();
            return;
        }

        var tints = new Dictionary<string, Color>();

        if (expandedLowMoisture && snapshot != null && twinDatabase != null)
        {
            var plants = twinDatabase.GetPlantsWhere((p, row) =>
                row != null && row.groundMoisture < OverviewRules.LowMoistureThreshold);
            foreach (var p in plants)
                if (p != null && !string.IsNullOrEmpty(p.plantId))
                    tints[p.plantId] = lowMoistureColor;
        }

        if (expandedBadHealth && snapshot?.badHealthPlants != null)
            foreach (var p in snapshot.badHealthPlants)
                if (!string.IsNullOrEmpty(p.plantId))
                    tints[p.plantId] = badHealthColor;

        if (expandedWarnings && snapshot?.warningPlants != null)
            foreach (var p in snapshot.warningPlants)
                if (!string.IsNullOrEmpty(p.plantId))
                    tints[p.plantId] = warningTagColor;

        if (expandedRipe && snapshot?.ripePlants != null)
            foreach (var p in snapshot.ripePlants)
                if (!string.IsNullOrEmpty(p.plantId))
                    tints[p.plantId] = ripeColor;

        plantVisualRegistry.ApplyProtectedSet(tints, false);
    }

    /// <summary>
    /// Clears all overview highlights. Call when leaving Overview mode.
    /// </summary>
    public void ClearHighlights()
    {
        plantVisualRegistry?.ResetAll();
    }
}
