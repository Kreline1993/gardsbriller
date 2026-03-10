using System.Collections.Generic;
using UnityEngine;

public sealed class OverviewModeState : ModeStateBase
{
    private readonly GameObject              rowOverlayPrefab;
    private readonly float                   rowOverlayHeight;

    private readonly GameObject              badHealthIconPrefab;
    private readonly GameObject              warningIconPrefab;
    private readonly GameObject              ripeIconPrefab;

    // Scene-assigned controller — set via ModeController Inspector field.
    // If null, a temporary one is created at runtime as a fallback.
    private readonly PlantIconLODController  sceneController;
    private          PlantIconLODController  runtimeController; // only used when sceneController is null

    private readonly List<GameObject> spawnedRowOverlays = new List<GameObject>();

    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(
        ModeContext             context,
        GameObject              rowOverlayPrefab    = null,
        float                   rowOverlayHeight    = 1.5f,
        GameObject              badHealthIconPrefab = null,
        GameObject              warningIconPrefab   = null,
        GameObject              ripeIconPrefab      = null,
        PlantIconLODController  lodController       = null)
        : base(context)
    {
        this.rowOverlayPrefab    = rowOverlayPrefab;
        this.rowOverlayHeight    = rowOverlayHeight;
        this.badHealthIconPrefab = badHealthIconPrefab;
        this.warningIconPrefab   = warningIconPrefab;
        this.ripeIconPrefab      = ripeIconPrefab;
        this.sceneController     = lodController;
    }

    public override void Enter()
    {
        if (context.PlantVisualRegistry == null)
        {
            Debug.LogWarning("[OverviewModeState] PlantVisualRegistry is null.");
            return;
        }

        SpawnRowOverlays();
        InitialiseLODController();
    }

    public override void Exit()
    {
        context.PlantVisualRegistry?.ResetAll();
        DestroyRowOverlays();
        ShutdownLODController();
    }

    // ── LOD controller lifecycle ─────────────────────────────────────────

    private void InitialiseLODController()
    {
        TwinDatabase        db       = context.TwinDatabase;
        PlantVisualRegistry registry = context.PlantVisualRegistry;
        if (db == null || registry == null) return;

        PlantIconLODController controller = GetOrCreateController();
        if (controller == null) return;

        // ── User transform ────────────────────────────────────────────────
        // Camera.main resolves to CenterEyeAnchor in a standard OVR rig
        // as long as that GameObject is tagged "MainCamera".
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[OverviewModeState] Camera.main is null – LOD distances will not update.");
            return;
        }

        controller.Initialise(cam.transform);

        // ── Condition layers ──────────────────────────────────────────────

        if (badHealthIconPrefab != null)
            RegisterLayer(controller, "bad_health", badHealthIconPrefab,
                db.GetPlantsWhere(p => p.healthStatus == OverviewRules.BadHealthStatus),
                registry);

        if (warningIconPrefab != null)
            RegisterLayer(controller, "warning", warningIconPrefab,
                db.GetPlantsWhere(p => p.notes != null && p.notes.noteTag == OverviewRules.WarningNoteTag),
                registry);

        if (ripeIconPrefab != null)
            RegisterLayer(controller, "ripe", ripeIconPrefab,
                db.GetPlantsWhere(p => p.growth >= 100),
                registry);

        controller.enabled = true;
    }

    private void ShutdownLODController()
    {
        // If using a scene object, just clear its data and disable its Update loop.
        // The GameObject stays in the scene for the next Enter() call.
        if (sceneController != null)
        {
            sceneController.ClearIcons();
            sceneController.ClearLayers();
            sceneController.enabled = false;
            return;
        }

        // If we created a temporary runtime controller, destroy it entirely.
        if (runtimeController != null)
        {
            UnityEngine.Object.Destroy(runtimeController.gameObject);
            runtimeController = null;
        }
    }

    /// <summary>
    /// Returns the scene controller if one was provided, otherwise creates a temporary
    /// runtime controller parented to TwinGenerator as a fallback.
    /// </summary>
    private PlantIconLODController GetOrCreateController()
    {
        if (sceneController != null)
        {
            sceneController.enabled = false; // disable until fully initialised
            return sceneController;
        }

        // Fallback: create a temporary controller at runtime
        TwinGenerator gen    = UnityEngine.Object.FindObjectOfType<TwinGenerator>();
        Transform     parent = gen != null ? gen.transform : null;

        GameObject go = new GameObject("[PlantIconLOD_Runtime]");
        if (parent != null)
            go.transform.SetParent(parent, worldPositionStays: false);

        runtimeController = go.AddComponent<PlantIconLODController>();
        runtimeController.enabled = false; // disable until fully initialised
        return runtimeController;
    }

    private static void RegisterLayer(
        PlantIconLODController controller,
        string                 layerKey,
        GameObject             prefab,
        List<Plant>            plants,
        PlantVisualRegistry    registry)
    {
        List<PlantIconLODController.PlantEntry> entries = new List<PlantIconLODController.PlantEntry>();

        foreach (Plant plant in plants)
        {
            if (!registry.HandlesByPlantId.TryGetValue(plant.plantId, out PlantVisualHandle handle)
                || handle == null)
                continue;

            var (bottomCentre, height) = handle.GetWorldBounds();
            entries.Add(new PlantIconLODController.PlantEntry
            {
                PlantId      = plant.plantId,
                BottomCentre = bottomCentre,
                Height       = height
            });
        }

        controller.AddLayer(layerKey, prefab, entries);
        Debug.Log($"[OverviewModeState] LOD layer '{layerKey}': {entries.Count} plants registered.");
    }

    // ── Row overlays (unchanged) ─────────────────────────────────────────

    private void SpawnRowOverlays()
    {
        if (rowOverlayPrefab == null) return;

        TwinDatabase db = context.TwinDatabase;
        if (db == null) return;

        TwinGenerator gen = UnityEngine.Object.FindObjectOfType<TwinGenerator>();
        if (gen == null)
        {
            Debug.LogWarning("[OverviewModeState] TwinGenerator not found – cannot spawn row overlays.");
            return;
        }

        List<Row> lowMoistureRows = db.GetRowsWhere(
            row => row.groundMoisture < OverviewRules.LowMoistureThreshold);

        foreach (Row row in lowMoistureRows)
        {
            if (row.location == null || row.size == null) continue;

            float s = gen.scaleFactor;
            float w = row.size.width  * s;
            float l = row.size.length * s;
            float h = rowOverlayHeight;

            Vector3 localBase   = new Vector3(row.location.x, row.location.y, row.location.z) * s;
            Vector3 localCenter = localBase + new Vector3(w / 2f, h / 2f, l / 2f);

            GameObject overlay = UnityEngine.Object.Instantiate(rowOverlayPrefab, gen.transform);
            overlay.transform.localPosition = localCenter;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale    = new Vector3(w, h, l);

            spawnedRowOverlays.Add(overlay);
            Debug.Log($"[OverviewModeState] Row overlay '{row.rowId}' | localPos={localCenter} | scale=({w:F2},{h:F2},{l:F2})");
        }
    }

    private void DestroyRowOverlays()
    {
        foreach (GameObject overlay in spawnedRowOverlays)
        {
            if (overlay != null) UnityEngine.Object.Destroy(overlay);
        }
        spawnedRowOverlays.Clear();
    }
}
