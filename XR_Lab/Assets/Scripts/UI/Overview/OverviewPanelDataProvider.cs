using System;
using System.Collections.Generic;
using UnityEngine;

public class OverviewPanelDataProvider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TwinDatabase twinDatabase;
    [SerializeField] private ModeController modeController;

    [Header("Refresh")]
    [SerializeField] private bool refreshOnlyInOverviewMode = true;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshIntervalSeconds = 1f;

    private float nextRefreshTime;

    public event Action<OverviewPanelDataSnapshot> DataUpdated;

    public OverviewPanelDataSnapshot CurrentData { get; private set; } = new OverviewPanelDataSnapshot();

    private void Awake()
    {
        if (twinDatabase == null)
            twinDatabase = FindFirstObjectByType<TwinDatabase>();

        if (modeController == null)
            modeController = FindFirstObjectByType<ModeController>();
    }

    private void OnEnable()
    {
        if (modeController != null)
            modeController.ModeChanged += HandleModeChanged;

        RefreshNow();
    }

    private void OnDisable()
    {
        if (modeController != null)
            modeController.ModeChanged -= HandleModeChanged;
    }

    private void Update()
    {
        if (!autoRefresh)
            return;

        if (Time.time < nextRefreshTime)
            return;

        if (refreshOnlyInOverviewMode && modeController != null && modeController.CurrentMode != AppMode.Overview)
            return;

        RefreshNow();
    }

    public void RefreshNow()
    {
        nextRefreshTime = Time.time + Mathf.Max(0.1f, refreshIntervalSeconds);

        CurrentData = BuildSnapshot();
        DataUpdated?.Invoke(CurrentData);
    }

    private void HandleModeChanged(AppMode mode)
    {
        if (!refreshOnlyInOverviewMode || mode == AppMode.Overview)
            RefreshNow();
    }

    private OverviewPanelDataSnapshot BuildSnapshot()
    {
        var snapshot = new OverviewPanelDataSnapshot();

        if (twinDatabase == null)
        {
            Debug.LogWarning("[OverviewPanelDataProvider] TwinDatabase is null");
            return snapshot;
        }

        List<Row> rows = twinDatabase.GetRowsWhere(row => row != null);
        
        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("[OverviewPanelDataProvider] No rows found in TwinDatabase");
            return snapshot;
        }

        int lowestMoisture = int.MaxValue;
        System.DateTime? earliestNextPesticide = null;
        System.DateTime? latestPesticide = null;
        System.DateTime? latestWatered = null;

        foreach (Row row in rows)
        {
            int plantCount = row.plants != null ? row.plants.Length : 0;

            snapshot.summary.totalRows++;
            snapshot.summary.totalPlants += plantCount;

            // Track lowest moisture
            if (row.groundMoisture < lowestMoisture)
                lowestMoisture = row.groundMoisture;

            if (row.groundMoisture < OverviewRules.LowMoistureThreshold)
            {
                snapshot.summary.lowMoistureRows++;
                snapshot.lowMoistureRows.Add(new OverviewRowSectionData
                {
                    rowId = row.rowId,
                    groundMoisture = row.groundMoisture,
                    plantCount = plantCount
                });
            }

            if (row.lastWateredDate != null)
            {
                try
                {
                    var rowWatered = ConvertDateDataToDateTime(row.lastWateredDate);
                    if (!latestWatered.HasValue || rowWatered > latestWatered)
                        latestWatered = rowWatered;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[OverviewPanelDataProvider] Failed to parse lastWateredDate for row {row.rowId}: {ex.Message}");
                }
            }

            if (row.plants == null)
                continue;

            foreach (Plant plant in row.plants)
            {
                if (plant == null)
                    continue;

                bool isBadHealth = string.Equals(
                    plant.healthStatus,
                    OverviewRules.BadHealthStatus,
                    StringComparison.OrdinalIgnoreCase);

                bool hasWarning = string.Equals(
                    plant.notes?.noteTag,
                    OverviewRules.WarningNoteTag,
                    StringComparison.OrdinalIgnoreCase);

                if (isBadHealth)
                {
                    snapshot.summary.badHealthPlants++;
                    snapshot.badHealthPlants.Add(new OverviewPlantSectionData
                    {
                        plantId = plant.plantId,
                        species = plant.species,
                        rowId = row.rowId,
                        growth = plant.growth,
                        healthStatus = plant.healthStatus,
                        noteTag = plant.notes?.noteTag
                    });
                }

                if (hasWarning)
                {
                    snapshot.summary.warningPlants++;
                    snapshot.warningPlants.Add(new OverviewPlantSectionData
                    {
                        plantId = plant.plantId,
                        species = plant.species,
                        rowId = row.rowId,
                        growth = plant.growth,
                        healthStatus = plant.healthStatus,
                        noteTag = plant.notes?.noteTag
                    });
                }

                if (plant.growth >= OverviewRules.RipeGrowthThreshold)
                {
                    snapshot.summary.ripePlants++;
                    snapshot.ripePlants.Add(new OverviewPlantSectionData
                    {
                        plantId = plant.plantId,
                        species = plant.species,
                        rowId = row.rowId,
                        growth = plant.growth,
                        healthStatus = plant.healthStatus,
                        noteTag = plant.notes?.noteTag
                    });
                }

                if (plant.lastPesticide != null)
                {
                    try
                    {
                        var pestDate = ConvertDateDataToDateTime(plant.lastPesticide);
                        if (!latestPesticide.HasValue || pestDate > latestPesticide)
                            latestPesticide = pestDate;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[OverviewPanelDataProvider] Failed to parse lastPesticide for plant {plant.plantId}: {ex.Message}");
                    }
                }

                // Track earliest next pesticide date
                if (plant.nextPesticide != null)
                {
                    try
                    {
                        var nextPestDate = ConvertDateDataToDateTime(plant.nextPesticide);
                        if (!earliestNextPesticide.HasValue || nextPestDate < earliestNextPesticide)
                            earliestNextPesticide = nextPestDate;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[OverviewPanelDataProvider] Failed to parse nextPesticide date for plant {plant.plantId}: {ex.Message}");
                    }
                }
            }
        }

        snapshot.lowMoistureRows.Sort((a, b) => string.Compare(a.rowId, b.rowId, StringComparison.Ordinal));
        snapshot.badHealthPlants.Sort((a, b) => string.Compare(a.plantId, b.plantId, StringComparison.Ordinal));
        snapshot.warningPlants.Sort((a, b) => string.Compare(a.plantId, b.plantId, StringComparison.Ordinal));
        snapshot.ripePlants.Sort((a, b) => string.Compare(a.plantId, b.plantId, StringComparison.Ordinal));

        snapshot.lowestRowMoisture = lowestMoisture == int.MaxValue ? 0 : lowestMoisture;
        snapshot.nextPesticidesDate = earliestNextPesticide.HasValue 
            ? earliestNextPesticide.Value.ToString("dd/MM/yyyy")
            : "N/A";
        snapshot.lastPesticideDate = latestPesticide.HasValue
            ? latestPesticide.Value.ToString("dd/MM/yyyy")
            : "N/A";
        snapshot.lastWateredDate = latestWatered.HasValue
            ? latestWatered.Value.ToString("dd/MM/yyyy")
            : "N/A";

#if UNITY_EDITOR
        Debug.Log($"[OverviewPanelDataProvider] Snapshot built - Rows: {snapshot.summary.totalRows}, Ripe: {snapshot.summary.ripePlants}, Lowest Moisture: {snapshot.lowestRowMoisture}%, Next Pesticide: {snapshot.nextPesticidesDate}");
#endif

        return snapshot;
    }

    private System.DateTime ConvertDateDataToDateTime(DateData date)
    {
        return new System.DateTime(date.year, date.month, date.day);
    }
}
