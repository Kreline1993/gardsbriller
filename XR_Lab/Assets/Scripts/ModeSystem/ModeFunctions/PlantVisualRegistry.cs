using System.Collections.Generic;
using UnityEngine;

public class PlantVisualRegistry : MonoBehaviour
{
    [SerializeField] private Transform searchRoot;

    private readonly Dictionary<string, PlantAnchor> anchorsByPlantId = new Dictionary<string, PlantAnchor>();
    private readonly Dictionary<string, PlantVisualHandle> handlesByPlantId = new Dictionary<string, PlantVisualHandle>();

    public IReadOnlyDictionary<string, PlantAnchor> AnchorsByPlantId => anchorsByPlantId;
    public IReadOnlyDictionary<string, PlantVisualHandle> HandlesByPlantId => handlesByPlantId;

    private void Awake()
    {
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        anchorsByPlantId.Clear();
        handlesByPlantId.Clear();

        Transform root = searchRoot != null ? searchRoot : transform;
        PlantAnchor[] anchors = root.GetComponentsInChildren<PlantAnchor>(true);

        Debug.Log($"[PlantVisualRegistry] Searching for plant anchors under '{root.name}'. Found {anchors.Length} anchors.");

        foreach (PlantAnchor anchor in anchors)
        {
            RegisterAnchor(anchor);
        }

        Debug.Log($"[PlantVisualRegistry] Indexed {anchorsByPlantId.Count} plants.");
    }

    public void RegisterAnchors(IEnumerable<PlantAnchor> anchors)
    {
        if (anchors == null)
            return;

        foreach (PlantAnchor anchor in anchors)
        {
            RegisterAnchor(anchor);
        }

        Debug.Log($"[PlantVisualRegistry] Indexed {anchorsByPlantId.Count} plants after anchor registration.");
    }

    public void RegisterAnchor(PlantAnchor anchor)
    {
        if (anchor == null || string.IsNullOrEmpty(anchor.PlantId))
            return;

        anchorsByPlantId[anchor.PlantId] = anchor;

        PlantVisualHandle handle = anchor.GetComponent<PlantVisualHandle>();
        if (handle == null)
            handle = anchor.gameObject.AddComponent<PlantVisualHandle>();

        handle.InitializeIfNeeded();
        handlesByPlantId[anchor.PlantId] = handle;
    }

    public bool TryGetAnchor(string plantId, out PlantAnchor anchor)
    {
        anchor = null;
        return !string.IsNullOrEmpty(plantId) && anchorsByPlantId.TryGetValue(plantId, out anchor);
    }

    public bool TryGetHandle(string plantId, out PlantVisualHandle handle)
    {
        handle = null;
        return !string.IsNullOrEmpty(plantId) && handlesByPlantId.TryGetValue(plantId, out handle);
    }

    public bool TryGetLiveHandle(string plantId, out PlantVisualHandle handle)
    {
        handle = null;
        return TryGetHandle(plantId, out handle)
            && TryGetAnchor(plantId, out PlantAnchor anchor)
            && anchor != null
            && anchor.HasActiveInteractable;
    }

    public bool HasSpawnedInteractable(string plantId)
    {
        return TryGetAnchor(plantId, out PlantAnchor anchor) && anchor != null && anchor.HasActiveInteractable;
    }

    public bool TryGetWorldBounds(string plantId, out Vector3 bottomCentre, out float height)
    {
        bottomCentre = default;
        height = 0f;

        if (!TryGetAnchor(plantId, out PlantAnchor anchor) || anchor == null)
            return false;

        (bottomCentre, height) = anchor.GetWorldBounds();
        return true;
    }

    /// <summary>
    /// Marks every indexed plant as protected. Use when all known plants should be
    /// shown as "don't touch" (e.g. weeding mode without weed-specific data).
    /// </summary>
    public void MarkAllProtected(Color protectedTint, bool disableTouch)
    {
        Debug.Log($"[PlantVisualRegistry] Marking all {handlesByPlantId.Count} plants as protected.");

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.SetProtectedVisual(true, protectedTint, disableTouch);
        }
    }

    /// <summary>
    /// Spawns a tinted overlay prefab at each plant's position.
    /// When <paramref name="hideOriginal"/> is true the original renderers are hidden;
    /// otherwise they remain visible underneath the overlay.
    /// When <paramref name="disableTouch"/> is true, colliders on the original plants
    /// are disabled (the overlay prefab retains its own colliders).
    /// </summary>
    public void MarkAllProtectedWithOverlay(GameObject overlayPrefab, Color tint, bool disableTouch, bool hideOriginal = true)
    {
        Debug.Log($"[PlantVisualRegistry] Spawning overlay for all {handlesByPlantId.Count} plants.");

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.SpawnOverlay(overlayPrefab, tint, hideOriginal);

            if (disableTouch)
                pair.Value.DisableColliders();
        }
    }

    /// <summary>
    /// Makes all indexed plants visible (alpha override) without changing their tint.
    /// </summary>
    public void ShowAll()
    {
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.SetVisible(true);
        }
    }

    public void ApplyProtectedSet(Dictionary<string, Color> protectedPlantTints, bool disableTouchForProtected)
    {
        if (protectedPlantTints == null)
            protectedPlantTints = new Dictionary<string, Color>();

        Debug.Log($"[PlantVisualRegistry] Applying protected set: {protectedPlantTints.Count} protected plants, disableTouch={disableTouchForProtected}");

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            PlantVisualHandle handle = pair.Value;
            if (handle == null)
                continue;

            bool isProtected = protectedPlantTints.TryGetValue(pair.Key, out Color tint);
            handle.SetProtectedVisual(isProtected, isProtected ? tint : Color.white, disableTouchForProtected);
        }
    }

    public void ResetAll()
    {
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.ResetVisuals();
        }
    }

    /// <summary>
    /// Disables colliders on plants not in the highlighted set; restores colliders on highlighted plants.
    /// When <paramref name="highlightedPlantIds"/> is empty, restores all colliders.
    /// Use when a section is expanded and only highlighted plants should be interactable.
    /// </summary>
    public void SetCollidersForHighlightedOnly(HashSet<string> highlightedPlantIds)
    {
        if (highlightedPlantIds == null)
        {
            RestoreAllColliders();
            return;
        }

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            if (highlightedPlantIds.Count == 0 || highlightedPlantIds.Contains(pair.Key))
                pair.Value.RestoreColliders();
            else
                pair.Value.DisableColliders();
        }
    }

    /// <summary>
    /// Restores colliders on all plants to their original state.
    /// </summary>
    public void RestoreAllColliders()
    {
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            pair.Value?.RestoreColliders();
        }
    }

    /// <summary>
    /// Applies an individual colour to each plant based on the provided map.
    /// Plants not present in <paramref name="alertColors"/> receive <paramref name="defaultColor"/>.
    /// </summary>
    public void ApplyPerPlantColors(
        Dictionary<string, Color> alertColors,
        Color defaultColor,
        bool disableTouch)
    {
        Debug.Log($"[PlantVisualRegistry] Applying per-plant colours to {handlesByPlantId.Count} plants.");

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            Color color = alertColors.TryGetValue(pair.Key, out Color alertColor)
                ? alertColor
                : defaultColor;

            pair.Value.SetProtectedVisual(true, color, disableTouch);
        }
    }

    /// <summary>
    /// Spawns a per-plant coloured overlay using the alert map.
    /// Plants not in <paramref name="alertColors"/> use <paramref name="defaultColor"/>.
    /// </summary>
    public void ApplyPerPlantColorsWithOverlay(
        GameObject overlayPrefab,
        Dictionary<string, Color> alertColors,
        Color defaultColor,
        bool disableTouch,
        bool hideOriginal = true)
    {
        Debug.Log($"[PlantVisualRegistry] Spawning per-plant alert overlays for {handlesByPlantId.Count} plants.");

        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            Color color = alertColors.TryGetValue(pair.Key, out Color alertColor)
                ? alertColor
                : defaultColor;

            pair.Value.SpawnOverlay(overlayPrefab, color, hideOriginal);

            if (disableTouch)
                pair.Value.DisableColliders();
        }
    }

    /// <summary>
    /// Spawns coloured overlays ONLY for plants present in the alert map.
    /// Plants with no alert condition are left untouched.
    /// </summary>
    public void ApplyAlertOverlaysOnly(
        GameObject overlayPrefab,
        Dictionary<string, Color> alertColors,
        bool disableTouch,
        bool hideOriginal = true)
    {
        Debug.Log($"[PlantVisualRegistry] Spawning alert overlays for {alertColors.Count} flagged plants.");

        foreach (KeyValuePair<string, Color> pair in alertColors)
        {
            if (!handlesByPlantId.TryGetValue(pair.Key, out PlantVisualHandle handle) || handle == null)
                continue;

            handle.SpawnOverlay(overlayPrefab, pair.Value, hideOriginal);

            if (disableTouch)
                handle.DisableColliders();
        }
    }

    /// <summary>
    /// Spawns a icon above appropriate plants/>.
    /// </summary>
    public void ApplyIcons(GameObject iconPrefab, HashSet<string> plantIds, float yOffset = 0.3f)
    {
        if (iconPrefab == null || plantIds == null)
            return;

        Debug.Log($"[PlantVisualRegistry] Spawning icons for {plantIds.Count} plants.");

        foreach (string plantId in plantIds)
        {
            if (!handlesByPlantId.TryGetValue(plantId, out PlantVisualHandle handle) || handle == null)
                continue;

            handle.SpawnIconAbove(iconPrefab, yOffset);
        }
    }

    /// <summary>
    /// Destroys the ripe icon on every indexed plant.
    /// </summary>
    public void RemoveAllIcons()
    {
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            pair.Value?.DestroyIcon();
        }
    }
}
