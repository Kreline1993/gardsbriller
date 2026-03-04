using System.Collections.Generic;
using UnityEngine;

public class PlantVisualRegistry : MonoBehaviour
{
    [SerializeField] private Transform searchRoot;

    private readonly Dictionary<string, PlantVisualHandle> handlesByPlantId = new Dictionary<string, PlantVisualHandle>();

    public IReadOnlyDictionary<string, PlantVisualHandle> HandlesByPlantId => handlesByPlantId;

    private void Awake()
    {
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        handlesByPlantId.Clear();

        Transform root = searchRoot != null ? searchRoot : transform;
        PlantIdentity[] identities = root.GetComponentsInChildren<PlantIdentity>(true);
        
        Debug.Log($"[PlantVisualRegistry] Searching for plants under '{root.name}'. Found {identities.Length} PlantIdentity components.");
        
        foreach (PlantIdentity identity in identities)
        {
            if (identity == null || string.IsNullOrEmpty(identity.plantId))
                continue;

            PlantVisualHandle handle = identity.GetComponent<PlantVisualHandle>();
            if (handle == null)
                handle = identity.gameObject.AddComponent<PlantVisualHandle>();

            handle.InitializeIfNeeded();
            handlesByPlantId[identity.plantId] = handle;
        }
        
        Debug.Log($"[PlantVisualRegistry] Indexed {handlesByPlantId.Count} plants.");
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

    /// <summary>
    /// Applies a protected set of plants to the visual registry.
    /// Note: This method is not used in the current implementation.
    /// </summary>
    public void ApplyProtectedSet(HashSet<string> protectedPlantIds, Color protectedTint, bool disableTouchForProtected)
    {
        if (protectedPlantIds == null)
            protectedPlantIds = new HashSet<string>();

        Debug.Log($"[PlantVisualRegistry] Applying protected set: {protectedPlantIds.Count} protected plants, tint={protectedTint}, disableTouch={disableTouchForProtected}");
        
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            PlantVisualHandle handle = pair.Value;
            if (handle == null)
                continue;

            bool isProtected = protectedPlantIds.Contains(pair.Key);
            handle.SetProtectedVisual(isProtected, protectedTint, disableTouchForProtected);
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
}
