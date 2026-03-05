using System.Collections.Generic;
using UnityEngine;

public sealed class OverviewModeState : ModeStateBase
{
    private readonly Color lowMoistureColor;
    private readonly Color badHealthColor;
    private readonly Color warningTagColor;
    private readonly GameObject overlayPrefab;
    private readonly bool hideOriginalDuringOverlay;
    private readonly float rowOverlayHeight;
    private readonly GameObject ripeIconPrefab;
    private readonly float ripeIconYOffset;

    private readonly List<GameObject> spawnedRowOverlays = new List<GameObject>();

    private const int LowMoistureThreshold = 30;

    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(
        ModeContext context,
        Color lowMoistureColor,
        Color badHealthColor,
        Color warningTagColor,
        GameObject overlayPrefab = null,
        bool hideOriginalDuringOverlay = true,
        float rowOverlayHeight = 1.5f,
        GameObject ripeIconPrefab = null,
        float ripeIconYOffset = 0.3f)
        : base(context)
    {
        this.lowMoistureColor = lowMoistureColor;
        this.badHealthColor = badHealthColor;
        this.warningTagColor = warningTagColor;
        this.overlayPrefab = overlayPrefab;
        this.hideOriginalDuringOverlay = hideOriginalDuringOverlay;
        this.rowOverlayHeight = rowOverlayHeight;
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

        Dictionary<string, Color> plantAlerts = BuildPlantAlertColorMap();

        if (overlayPrefab != null)
        {
            context.PlantVisualRegistry.ApplyAlertOverlaysOnly(
                overlayPrefab, plantAlerts, false, hideOriginalDuringOverlay);

            SpawnRowOverlays();
        }
        else
        {
            context.PlantVisualRegistry.ApplyPerPlantColors(plantAlerts, Color.white, false);
        }

        // Ripe icons — runs regardless of overlayPrefab
        if (ripeIconPrefab != null)
            SpawnRipeIcons();
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
        DestroyRowOverlays();
        context.PlantVisualRegistry?.RemoveAllIcons();
    }

    /// <summary>
    /// Per-plant alerts only: bad health (orange) and warning tag (red).
    /// Low moisture is handled at row level via SpawnRowOverlays().
    /// </summary>
    private Dictionary<string, Color> BuildPlantAlertColorMap()
    {
        var map = new Dictionary<string, Color>();

        TwinDatabase db = context.TwinDatabase;
        if (db == null)
        {
            Debug.LogWarning("[OverviewModeState] TwinDatabase not found – skipping plant alert colouring.");
            return map;
        }

        // Orange: bad health
        List<Plant> badHealthPlants = db.GetPlantsWhere(
            plant => plant.healthStatus == "bad");
        foreach (Plant plant in badHealthPlants)
            map[plant.plantId] = badHealthColor;

        // Red: warning tag (overwrites orange)
        List<Plant> warningPlants = db.GetPlantsWhere(
            plant => plant.notes != null && plant.notes.noteTag == "warning");
        foreach (Plant plant in warningPlants)
            map[plant.plantId] = warningTagColor;

        Debug.Log($"[OverviewModeState] Plant alert map: {map.Count} plants flagged.");
        return map;
    }

    /// <summary>
    /// Spawns a single row-sized overlay for every row with groundMoisture below threshold.
    /// </summary>
    private void SpawnRowOverlays()
    {
        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        TwinGenerator gen = Object.FindObjectOfType<TwinGenerator>();
        if (gen == null)
        {
            Debug.LogWarning("[OverviewModeState] TwinGenerator not found – cannot spawn row overlays.");
            return;
        }

        List<Row> lowMoistureRows = db.GetRowsWhere(
            row => row.groundMoisture < LowMoistureThreshold);

        foreach (Row row in lowMoistureRows)
        {
            if (row.location == null || row.size == null) continue;

            float s = gen.scaleFactor;
            float w = row.size.width * s;
            float l = row.size.length * s;
            float h = rowOverlayHeight;

            Vector3 localBase = new Vector3(row.location.x, row.location.y, row.location.z) * s;
            Vector3 localCenter = localBase + new Vector3(w / 2f, h / 2f, l / 2f);

            // Instantiate as a child of TwinGenerator so local space matches how plants are spawned
            GameObject overlay = Object.Instantiate(overlayPrefab, gen.transform);
            overlay.transform.localPosition = localCenter;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = new Vector3(w, h, l);

            // Tint purple
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer rend in overlay.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(block);
                Material mat = rend.sharedMaterial;
                if (mat != null && mat.HasProperty("_BaseColor")) block.SetColor("_BaseColor", lowMoistureColor);
                if (mat != null && mat.HasProperty("_Color")) block.SetColor("_Color", lowMoistureColor);
                rend.SetPropertyBlock(block);
            }

            spawnedRowOverlays.Add(overlay);
            Debug.Log($"[OverviewModeState] Row overlay for '{row.rowId}' | localPos={localCenter} | scale=({w:F2}, {h:F2}, {l:F2})");
        }
    }

    private void DestroyRowOverlays()
    {
        foreach (GameObject overlay in spawnedRowOverlays)
        {
            if (overlay != null)
                Object.Destroy(overlay);
        }
        spawnedRowOverlays.Clear();
    }
    /// <summary>
    /// Spawns the ripe icon above every plant with growth >= 100.
    /// </summary>
    private void SpawnRipeIcons()
    {
        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        List<Plant> ripePlants = db.GetPlantsWhere(plant => plant.growth >= 100);

        var ripeIds = new HashSet<string>();
        foreach (Plant plant in ripePlants)
            ripeIds.Add(plant.plantId);

        context.PlantVisualRegistry.ApplyRipeIcons(ripeIconPrefab, ripeIds, ripeIconYOffset);
        Debug.Log($"[OverviewModeState] Ripe icon spawned for {ripeIds.Count} plants (growth >= 100).");
    }
}