using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages distance-based icon LOD and radius clustering for plant condition icons
/// while the app is in Overview mode.
///
/// Zone behaviour (distance measured from userTransform to each plant's world position):
///   Near  (distance &lt; nearThreshold)                  — individual icon at 75 % of plant height
///   Mid   (nearThreshold ≤ distance &lt; farThreshold)   — individual icon at plant top
///   Far   (distance ≥ farThreshold)                   — radius-clustered icon, scale ∝ plant count
///
/// Each condition (bad health, warning, ripe) is a separate layer so that icons from
/// different conditions never merge into the same cluster.
/// </summary>
[DisallowMultipleComponent]
public class PlantIconLODController : MonoBehaviour
{
    // ── Distance thresholds ─────────────────────────────────────────────

    [Header("Distance Thresholds (metres)")]
    public float nearThreshold = 3f;
    public float farThreshold  = 5f;

    [Header("Hysteresis (metres – half-band per boundary)")]
    [Tooltip("A plant must travel this far past a threshold before its zone changes. Prevents flickering.")]
    public float hysteresisBand = 0.15f;

    // ── Clustering ──────────────────────────────────────────────────────

    [Header("Clustering")]
    [Tooltip("World-space radius within which far-zone plants are merged into one cluster icon.")]
    public float clusterRadius = 2f;

    // ── Icon scale ──────────────────────────────────────────────────────

    [Header("Icon Scale")]
    [Tooltip("Uniform world-space scale used for single-plant icons (near + mid zones).")]
    public float singlePlantScale = 0.5f;

    [Tooltip("Scale of a cluster icon that represents exactly one plant.")]
    public float clusterMinScale = 0.3f;

    [Tooltip("Scale of a cluster icon when plant count >= maxCountForMaxScale.")]
    public float clusterMaxScale = 1.5f;

    [Tooltip("Plant count at which the cluster icon reaches clusterMaxScale.")]
    public int maxCountForMaxScale = 10;

    // ── Icon Y offset ────────────────────────────────────────────────────

    [Header("Icon Y Offset (metres above calculated position)")]
    [Tooltip("Extra height added to icons in the Near zone (individual, 75 % of plant height).")]
    public float nearIconYOffset    = 0.1f;

    [Tooltip("Extra height added to icons in the Mid zone (individual, plant top).")]
    public float midIconYOffset     = 0.1f;

    [Tooltip("Extra height added to cluster icons in the Far zone (above tallest plant in cluster).")]
    public float clusterIconYOffset = 0.15f;

    // ── Performance ─────────────────────────────────────────────────────

    [Header("Performance")]
    [Tooltip("Seconds between full LOD recalculations. Lower = more responsive, higher = cheaper.")]
    public float updateInterval = 0.1f;

    // ── Runtime ─────────────────────────────────────────────────────────

    private Transform userTransform;

    private readonly List<ConditionLayer> layers      = new List<ConditionLayer>();
    private HashSet<string>               visibleLayerKeys; // null = show all layers; empty = show none
    private readonly List<GameObject>     activeIcons = new List<GameObject>();

    // Zone memory for hysteresis. Key = "<layerIndex>_<plantId>" to keep layers independent.
    private readonly Dictionary<string, Zone> zoneMemory = new Dictionary<string, Zone>();

    private float timeSinceLastUpdate = float.MaxValue; // forces rebuild on first frame

    // ── Public data types ───────────────────────────────────────────────

    /// <summary>
    /// Describes one plant inside a condition layer.
    /// Populate from <see cref="PlantVisualHandle.GetWorldBounds"/> in OverviewModeState.
    /// </summary>
    public class PlantEntry
    {
        public string  PlantId;
        public Vector3 BottomCentre; // world-space bottom-centre of the plant bounding box
        public float   Height;       // full height of the plant in world units
    }

    // ── Internal types ──────────────────────────────────────────────────

    private class ConditionLayer
    {
        public string           LayerKey;
        public GameObject       Prefab;
        public List<PlantEntry> Plants;
    }

    private enum Zone { Near, Mid, Far }

    private struct ClusterResult
    {
        public Vector3 HorizontalCentre; // XZ centroid (Y is handled separately)
        public float   MaxTopY;          // highest plant top within this cluster
        public int     Count;
    }

    // ── Initialisation API ──────────────────────────────────────────────

    /// <summary>
    /// Called by OverviewModeState.Enter() each time overview mode is activated.
    /// Pass the XR camera transform (or Camera.main.transform) as <paramref name="user"/>.
    /// Safe to call multiple times — resets internal state before re-registering layers.
    /// </summary>
    public void Initialise(Transform user)
    {
        userTransform = user;
    }

    /// <summary>
    /// Register one condition layer (bad health, warning, ripe, etc.).
    /// Call once per condition after Initialise(). <paramref name="layerKey"/> must be unique.
    /// </summary>
    public void AddLayer(string layerKey, GameObject prefab, List<PlantEntry> plants)
    {
        layers.Add(new ConditionLayer
        {
            LayerKey = layerKey,
            Prefab   = prefab,
            Plants   = plants
        });
    }

    /// <summary>
    /// Removes all registered condition layers and clears zone memory.
    /// Called by OverviewModeState.Exit() so the controller is clean for the next Enter().
    /// Does not destroy icons — call ClearIcons() first if needed.
    /// </summary>
    public void ClearLayers()
    {
        layers.Clear();
        zoneMemory.Clear();
        visibleLayerKeys = null;
        timeSinceLastUpdate = float.MaxValue; // force a fresh rebuild on next Enter
    }

    /// <summary>
    /// Restricts which layers are shown. null = show all; empty = show none.
    /// Used with overview panel "hide icons for non-highlighted rule" toggle.
    /// </summary>
    public void SetVisibleLayers(IEnumerable<string> layerKeys)
    {
        visibleLayerKeys = layerKeys == null ? null : new HashSet<string>(layerKeys);
        timeSinceLastUpdate = float.MaxValue; // force immediate rebuild on next Update
    }

    // ── Unity lifecycle ─────────────────────────────────────────────────

    private void Update()
    {
        if (userTransform == null || layers.Count == 0) return;

        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate < updateInterval) return;

        timeSinceLastUpdate = 0f;
        RebuildAllIcons();
    }

    private void OnDisable() => ClearIcons();

    // ── Icon management ─────────────────────────────────────────────────

    public void ClearIcons()
    {
        foreach (GameObject icon in activeIcons)
        {
            if (icon != null) Destroy(icon);
        }
        activeIcons.Clear();
    }

    // ── Rebuild ─────────────────────────────────────────────────────────

    private void RebuildAllIcons()
    {
        ClearIcons();

        Vector3 userPos = userTransform.position;

        for (int li = 0; li < layers.Count; li++)
        {
            ConditionLayer layer = layers[li];
            if (layer.Prefab == null || layer.Plants == null || layer.Plants.Count == 0) continue;
            if (visibleLayerKeys != null && !visibleLayerKeys.Contains(layer.LayerKey)) continue;

            List<PlantEntry> nearList = new List<PlantEntry>();
            List<PlantEntry> midList  = new List<PlantEntry>();
            List<PlantEntry> farList  = new List<PlantEntry>();

            foreach (PlantEntry plant in layer.Plants)
            {
                float  dist = Vector3.Distance(userPos, plant.BottomCentre);
                string key  = $"{li}_{plant.PlantId}";
                Zone   zone = ResolveZone(key, dist);

                switch (zone)
                {
                    case Zone.Near: nearList.Add(plant); break;
                    case Zone.Mid:  midList.Add(plant);  break;
                    case Zone.Far:  farList.Add(plant);  break;
                }
            }

            // Near: icon at 75 % of plant height + nearIconYOffset
            foreach (PlantEntry p in nearList)
                SpawnIcon(layer.Prefab, IconWorldPos(p, 0.75f, nearIconYOffset), singlePlantScale);

            // Mid: icon at plant top (100 %) + midIconYOffset
            foreach (PlantEntry p in midList)
                SpawnIcon(layer.Prefab, IconWorldPos(p, 1.0f, midIconYOffset), singlePlantScale);

            // Far: radius-clustered, icon scale reflects plant count, + clusterIconYOffset
            foreach (ClusterResult cluster in BuildClusters(farList))
            {
                Vector3 pos   = new Vector3(cluster.HorizontalCentre.x,
                                            cluster.MaxTopY + clusterIconYOffset,
                                            cluster.HorizontalCentre.z);
                float   scale = ClusterScale(cluster.Count);
                SpawnIcon(layer.Prefab, pos, scale);
            }
        }
    }

    // ── Zone resolution with hysteresis ─────────────────────────────────

    private Zone ResolveZone(string key, float dist)
    {
        if (!zoneMemory.TryGetValue(key, out Zone prev))
        {
            Zone initial = RawZone(dist);
            zoneMemory[key] = initial;
            return initial;
        }

        Zone next = prev;

        switch (prev)
        {
            case Zone.Near:
                if (dist > nearThreshold + hysteresisBand) next = Zone.Mid;
                break;

            case Zone.Mid:
                if (dist < nearThreshold - hysteresisBand)   next = Zone.Near;
                else if (dist > farThreshold + hysteresisBand) next = Zone.Far;
                break;

            case Zone.Far:
                if (dist < farThreshold - hysteresisBand) next = Zone.Mid;
                break;
        }

        zoneMemory[key] = next;
        return next;
    }

    private Zone RawZone(float dist)
    {
        if (dist < nearThreshold) return Zone.Near;
        if (dist < farThreshold)  return Zone.Mid;
        return Zone.Far;
    }

    // ── Radius clustering ────────────────────────────────────────────────

    /// <summary>
    /// Greedy single-pass radius clustering.
    /// Each plant seeds its own cluster and absorbs any unassigned plant within clusterRadius.
    /// Order-dependent but fast and deterministic for a given plant list.
    /// </summary>
    private List<ClusterResult> BuildClusters(List<PlantEntry> plants)
    {
        List<ClusterResult> results  = new List<ClusterResult>();
        bool[]              assigned = new bool[plants.Count];

        for (int i = 0; i < plants.Count; i++)
        {
            if (assigned[i]) continue;

            assigned[i] = true;
            List<PlantEntry> members = new List<PlantEntry> { plants[i] };

            for (int j = i + 1; j < plants.Count; j++)
            {
                if (assigned[j]) continue;

                float d = Vector3.Distance(plants[i].BottomCentre, plants[j].BottomCentre);
                if (d <= clusterRadius)
                {
                    members.Add(plants[j]);
                    assigned[j] = true;
                }
            }

            results.Add(ComputeCluster(members));
        }

        return results;
    }

    private static ClusterResult ComputeCluster(List<PlantEntry> members)
    {
        Vector3 xzSum  = Vector3.zero;
        float   maxTop = float.MinValue;

        foreach (PlantEntry p in members)
        {
            xzSum  += p.BottomCentre;
            float top = p.BottomCentre.y + p.Height;
            if (top > maxTop) maxTop = top;
        }

        return new ClusterResult
        {
            HorizontalCentre = xzSum / members.Count,
            MaxTopY          = maxTop,
            Count            = members.Count
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static Vector3 IconWorldPos(PlantEntry plant, float heightFraction, float yOffset = 0f)
    {
        return new Vector3(
            plant.BottomCentre.x,
            plant.BottomCentre.y + plant.Height * heightFraction + yOffset,
            plant.BottomCentre.z);
    }

    private void SpawnIcon(GameObject prefab, Vector3 worldPos, float uniformScale)
    {
        GameObject icon = Instantiate(prefab, worldPos, Quaternion.identity, transform);
        icon.transform.localScale = Vector3.one * uniformScale;
        activeIcons.Add(icon);
    }

    private float ClusterScale(int count)
    {
        if (maxCountForMaxScale <= 1) return clusterMaxScale;
        float t = Mathf.Clamp01((float)(count - 1) / (maxCountForMaxScale - 1));
        return Mathf.Lerp(clusterMinScale, clusterMaxScale, t);
    }
}
