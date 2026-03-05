using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class OverviewModeState : ModeStateBase
{
    private readonly GameObject rowOverlayPrefab;
    private readonly float rowOverlayHeight;

    private readonly GameObject badHealthIconPrefab;
    private readonly float badHealthIconYOffset;

    private readonly GameObject warningIconPrefab;
    private readonly float warningIconYOffset;

    private readonly GameObject ripeIconPrefab;
    private readonly float ripeIconYOffset;

    private readonly List<GameObject> spawnedRowOverlays = new List<GameObject>();

    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(
        ModeContext context,
        GameObject rowOverlayPrefab = null,
        float rowOverlayHeight = 1.5f,
        GameObject badHealthIconPrefab = null,
        float badHealthIconYOffset = 0.3f,
        GameObject warningIconPrefab = null,
        float warningIconYOffset = 0.3f,
        GameObject ripeIconPrefab = null,
        float ripeIconYOffset = 0.3f)
        : base(context)
    {
        this.rowOverlayPrefab = rowOverlayPrefab;
        this.rowOverlayHeight = rowOverlayHeight;
        this.badHealthIconPrefab = badHealthIconPrefab;
        this.badHealthIconYOffset = badHealthIconYOffset;
        this.warningIconPrefab = warningIconPrefab;
        this.warningIconYOffset = warningIconYOffset;
        this.ripeIconPrefab = ripeIconPrefab;
        this.ripeIconYOffset = ripeIconYOffset;
    }

    public override void Enter()
    {
        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[OverviewModeState] PlantVisualRegistry is null.");
            return;
        }

        SpawnRowOverlays();

        if (badHealthIconPrefab != null)
            SpawnBadHealthIcons();

        if (warningIconPrefab != null)
            SpawnWarningIcons();

        if (ripeIconPrefab != null)
            SpawnRipeIcons();
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
        DestroyRowOverlays();
        context.PlantVisualRegistry?.RemoveAllIcons();
    }

    private void SpawnBadHealthIcons()
    {
        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        List<Plant> plants = db.GetPlantsWhere(plant => plant.healthStatus == "bad");

        HashSet<string> ids = new HashSet<string>();
        foreach (Plant plant in plants)
            ids.Add(plant.plantId);

        context.PlantVisualRegistry.ApplyIcons(badHealthIconPrefab, ids, badHealthIconYOffset);
        Debug.Log($"[OverviewModeState] Bad health icons spawned for {ids.Count} plants.");
    }

    private void SpawnWarningIcons()
    {
        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        List<Plant> plants = db.GetPlantsWhere(
            plant => plant.notes != null && plant.notes.noteTag == "warning");

        HashSet<string> ids = new HashSet<string>();
        foreach (Plant plant in plants)
            ids.Add(plant.plantId);

        context.PlantVisualRegistry.ApplyIcons(warningIconPrefab, ids, warningIconYOffset);
        Debug.Log($"[OverviewModeState] Warning icons spawned for {ids.Count} plants.");
    }

    private void SpawnRipeIcons()
    {
        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        List<Plant> plants = db.GetPlantsWhere(plant => plant.growth >= 100);

        HashSet<string> ids = new HashSet<string>();
        foreach (Plant plant in plants)
            ids.Add(plant.plantId);

        context.PlantVisualRegistry.ApplyIcons(ripeIconPrefab, ids, ripeIconYOffset);
        Debug.Log($"[OverviewModeState] Ripe icons spawned for {ids.Count} plants (growth >= 100).");
    }

    /// <summary>
    /// Spawns a single row-sized overlay for every row with groundMoisture below threshold.
    /// </summary>
    private void SpawnRowOverlays()
    {
        if (rowOverlayPrefab == null) return;

        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        TwinGenerator gen = UnityEngine.Object.FindObjectOfType<TwinGenerator>();
        if (gen == null)
        {
            Debug.LogWarning("[OverviewModeState] TwinGenerator not found - cannot spawn row overlays.");
            return;
        }

        List<Row> lowMoistureRows = db.GetRowsWhere(
            row => row.groundMoisture < OverviewRules.LowMoistureThreshold);

        foreach (Row row in lowMoistureRows)
        {
            if (row.location == null || row.size == null) continue;

            float s = gen.scaleFactor;
            float w = row.size.width * s;
            float l = row.size.length * s;
            float h = rowOverlayHeight;

            Vector3 localBase = new Vector3(row.location.x, row.location.y, row.location.z) * s;
            Vector3 localCenter = localBase + new Vector3(w / 2f, h / 2f, l / 2f);

            GameObject overlay = UnityEngine.Object.Instantiate(rowOverlayPrefab, gen.transform);
            overlay.transform.localPosition = localCenter;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = new Vector3(w, h, l);

            spawnedRowOverlays.Add(overlay);
            Debug.Log($"[OverviewModeState] Row overlay for '{row.rowId}' | localPos={localCenter} | scale=({w:F2}, {h:F2}, {l:F2})");
        }
    }

    private void DestroyRowOverlays()
    {
        foreach (GameObject overlay in spawnedRowOverlays)
        {
            if (overlay != null)
                UnityEngine.Object.Destroy(overlay);
        }
        spawnedRowOverlays.Clear();
    }
}