public static class OverviewRules
{
    // ── Row overlays ─────────────────────────────────────────────────────
    public const int    LowMoistureThreshold = 30;

    // ── Plant condition filters ───────────────────────────────────────────
    public const string BadHealthStatus = "bad";
    public const string WarningNoteTag  = "warning";

    // ── Icon LOD defaults (mirror PlantIconLODController inspector defaults) ──
    public const float NearThreshold    = 3f;    // < 3 m  → individual icon at 75 % height
    public const float FarThreshold     = 5f;    // 3–5 m  → individual icon at top
                                                  // > 5 m  → radius cluster
    public const float HysteresisBand   = 0.15f; // metres either side of each threshold
    public const float ClusterRadius    = 2f;     // world-space cluster merge radius
}
