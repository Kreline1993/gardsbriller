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
            twinDatabase = FindObjectOfType<TwinDatabase>();

        if (modeController == null)
            modeController = FindObjectOfType<ModeController>();
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
            return snapshot;

        List<Row> rows = twinDatabase.GetRowsWhere(row => row != null);
        foreach (Row row in rows)
        {
            int plantCount = row.plants != null ? row.plants.Length : 0;

            snapshot.summary.totalRows++;
            snapshot.summary.totalPlants += plantCount;

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
            }
        }

        snapshot.lowMoistureRows.Sort((a, b) => string.Compare(a.rowId, b.rowId, StringComparison.Ordinal));
        snapshot.badHealthPlants.Sort((a, b) => string.Compare(a.plantId, b.plantId, StringComparison.Ordinal));
        snapshot.warningPlants.Sort((a, b) => string.Compare(a.plantId, b.plantId, StringComparison.Ordinal));

        return snapshot;
    }
}
