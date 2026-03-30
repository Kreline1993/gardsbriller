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
    [SerializeField] private PlantIconLODController iconLODController;

    [Header("Icon Filter")]
    [Tooltip("When enabled, only icons for the currently highlighted rule are shown. Icons for other rules are hidden.")]
    [SerializeField] private bool hideIconsForNonHighlightedRule = false;

    [Header("Interaction")]
    [Tooltip("When enabled and a section is expanded, only highlighted plants are interactable. Non-highlighted plants have their colliders disabled.")]
    [SerializeField] private bool disableInteractionForNonHighlighted = false;

    [Header("Colors")]
    [SerializeField] private Color lowMoistureColor = new Color(0.5f, 0f, 1f, 1f);
    [SerializeField] private Color badHealthColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color warningTagColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private Color ripeColor = new Color(0f, 0.8f, 0.2f, 1f);

    private HashSet<string> _lastHighlightedIds;

    private void Awake()
    {
        if (plantVisualRegistry == null) plantVisualRegistry = GetComponent<PlantVisualRegistry>();
        if (plantVisualRegistry == null) plantVisualRegistry = FindFirstObjectByType<PlantVisualRegistry>();
        if (twinDatabase == null) twinDatabase = FindFirstObjectByType<TwinDatabase>();
        if (modeController == null) modeController = FindFirstObjectByType<ModeController>();
        if (iconLODController == null) iconLODController = FindFirstObjectByType<PlantIconLODController>();
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

        // Interaction filter: when enabled and a section is expanded, only highlighted plants are interactable
        if (disableInteractionForNonHighlighted)
        {
            var highlightedIds = new HashSet<string>(tints.Keys);
            plantVisualRegistry.SetCollidersForHighlightedOnly(highlightedIds);
            // Only close panels when the highlighted set has changed (avoids closing on every data refresh ~1s)
            if (highlightedIds.Count > 0)
            {
                bool setChanged = _lastHighlightedIds == null || !highlightedIds.SetEquals(_lastHighlightedIds);
                _lastHighlightedIds = new HashSet<string>(highlightedIds);
                if (setChanged)
                    InfoPanelSpawner.ClosePanelsForNonHighlighted(highlightedIds);
            }
        }
        else
        {
            plantVisualRegistry.RestoreAllColliders();
            _lastHighlightedIds = null;
        }

        // Icon filter: when enabled, only show icons for the currently highlighted rule(s)
        if (iconLODController != null && hideIconsForNonHighlightedRule)
        {
            var visibleKeys = new List<string>();
            if (expandedBadHealth) visibleKeys.Add("bad_health");
            if (expandedWarnings) visibleKeys.Add("warning");
            if (expandedRipe) visibleKeys.Add("ripe");
            // Low moisture has no plant-level icons (only row overlays)

            if (visibleKeys.Count == 0)
            {
                if (expandedLowMoisture)
                    iconLODController.SetVisibleLayers(new List<string>()); // low moisture has no plant icons: hide all
                else
                    iconLODController.SetVisibleLayers(null); // no section expanded: show all
            }
            else
            {
                iconLODController.SetVisibleLayers(visibleKeys);
            }
        }
        else if (iconLODController != null)
        {
            iconLODController.SetVisibleLayers(null); // show all when toggle is off
        }
    }

    /// <summary>
    /// Clears all overview highlights. Call when leaving Overview mode.
    /// </summary>
    public void ClearHighlights()
    {
        plantVisualRegistry?.ResetAll();
    }
}
