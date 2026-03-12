using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages distance-based visual switching for selected ripe plants in Picking mode.
/// Plants within "Distance Threshold" show a spawned icon above them;
/// plants beyond it receive their species colour tint.
/// Hysteresis prevents flickering when the user moves near the boundary.
/// </summary>
[DisallowMultipleComponent]
public class PickingProximityController : MonoBehaviour
{
    [Header("Distance Threshold (metres)")]
    [Tooltip("Plants closer than this distance switch from colour tint to icon.")]
    public float overlayThreshold = 3f;

    [Header("Hysteresis (metres – half-band per boundary)")]
    [Tooltip("A plant must travel this far past the threshold before its zone changes. Prevents flickering.")]
    public float hysteresisBand = 0.15f;

    [Header("Performance")]
    [Tooltip("Seconds between distance checks. Lower = more responsive, higher = cheaper.")]
    public float updateInterval = 0.5f;

    [Header("Icon Placement")]
    [Tooltip("Extra height above the plant's top bounds where the icon prefab is placed.")]
    public float iconYOffset = 0.1f;

    // --- Runtime -------

    private Transform _userTransform;
    private PlantVisualRegistry _registry;
    private Dictionary<string, GameObject> _overlayPrefabs;
    private Dictionary<string, Color> _speciesTints;

    private readonly List<PlantEntry> _selectedPlants = new List<PlantEntry>();
    private readonly Dictionary<string, ProximityZone> _zoneMemory = new Dictionary<string, ProximityZone>();

    private float _timeSinceLastUpdate = float.MaxValue;

    // --- Internal types -----

    private enum ProximityZone { Overlay, Tint }

    private class PlantEntry
    {
        public string PlantId;
        public string Species;
        public Vector3 BottomCentre;
    }

    // --- Public API -------

    /// <summary>
    /// Called by PlantPickingModeState.Enter() each time picking mode is activated.
    /// Pass Camera.main.transform as <paramref name="user"/>.
    /// </summary>
    public void Initialise(Transform user, PlantVisualRegistry registry,
                           Dictionary<string, GameObject> overlayPrefabs, Dictionary<string, Color> speciesTints)
    {
        _userTransform = user;
        _registry = registry;
        _overlayPrefabs = overlayPrefabs;
        _speciesTints = speciesTints;
        _timeSinceLastUpdate = float.MaxValue;
    }

    /// <summary>
    /// Updates the set of selected plants. Resets visuals for any plant no longer in the
    /// selection, and forces an immediate proximity recalculation.
    /// </summary>
    public void SetSelectedPlants(List<Plant> plants)
    {
        if (_registry == null) return;

        var newIds = new HashSet<string>();
        foreach (Plant p in plants)
            newIds.Add(p.plantId);

        // Reset any plant that has been deselected
        foreach (PlantEntry entry in _selectedPlants)
        {
            if (newIds.Contains(entry.PlantId)) continue;

            if (_registry.HandlesByPlantId.TryGetValue(entry.PlantId, out PlantVisualHandle handle))
            {
                handle.DestroyIcon();
                handle.SetProtectedVisual(false, Color.white, false);
            }
            _zoneMemory.Remove(entry.PlantId);
        }

        // Rebuild entry list
        _selectedPlants.Clear();
        foreach (Plant plant in plants)
        {
            if (!_registry.HandlesByPlantId.TryGetValue(plant.plantId, out PlantVisualHandle handle))
                continue;

            (Vector3 bottomCentre, float _) = handle.GetWorldBounds();
            _selectedPlants.Add(new PlantEntry
            {
                PlantId = plant.plantId,
                Species = plant.species,
                BottomCentre = bottomCentre
            });
        }

        _timeSinceLastUpdate = float.MaxValue; // force immediate update
    }

    /// <summary>
    /// Resets all visuals and clears plant tracking. Called by PlantPickingModeState.Exit().
    /// </summary>
    public void ClearPlants()
    {
        if (_registry != null)
        {
            foreach (PlantEntry entry in _selectedPlants)
            {
                if (_registry.HandlesByPlantId.TryGetValue(entry.PlantId, out PlantVisualHandle handle))
                {
                    handle.DestroyIcon();
                    handle.SetProtectedVisual(false, Color.white, false);
                }
            }
        }

        _selectedPlants.Clear();
        _zoneMemory.Clear();
    }

    // --- Unity lifecycle -----

    private void Update()
    {
        if (_userTransform == null || _registry == null || _selectedPlants.Count == 0) return;

        _timeSinceLastUpdate += Time.deltaTime;
        if (_timeSinceLastUpdate < updateInterval) return;

        _timeSinceLastUpdate = 0f;
        UpdateProximityVisuals();
    }

    private void OnDisable() => ClearPlants();

    // --- Proximity logic -----

    private void UpdateProximityVisuals()
    {
        Vector3 userPos = _userTransform.position;

        foreach (PlantEntry entry in _selectedPlants)
        {
            if (!_registry.HandlesByPlantId.TryGetValue(entry.PlantId, out PlantVisualHandle handle))
                continue;

            float dist = Vector3.Distance(userPos, entry.BottomCentre);

            // Read previous zone BEFORE ResolveZone writes to _zoneMemory
            bool hadZone = _zoneMemory.TryGetValue(entry.PlantId, out ProximityZone prevZone);
            ProximityZone newZone = ResolveZone(entry.PlantId, dist);

            if (hadZone && prevZone == newZone) continue; // no transition, skip

            ApplyZone(handle, entry.Species, newZone);
        }
    }

    private void ApplyZone(PlantVisualHandle handle, string species, ProximityZone zone)
    {
        if (zone == ProximityZone.Overlay)
        {
            GameObject prefab = _overlayPrefabs != null && _overlayPrefabs.TryGetValue(species, out GameObject p)
                ? p : null;

            handle.SetProtectedVisual(false, Color.white, false); // remove tint
            handle.SpawnIconAbove(prefab, iconYOffset);           // place icon at plant top
        }
        else // Tint
        {
            handle.DestroyIcon();

            Color tint = _speciesTints != null && _speciesTints.TryGetValue(species, out Color c)
                ? c : Color.white;
            handle.SetProtectedVisual(true, tint, false);
        }
    }

    // --- Zone resolution with hysteresis -----

    private ProximityZone ResolveZone(string plantId, float dist)
    {
        if (!_zoneMemory.TryGetValue(plantId, out ProximityZone prev))
        {
            ProximityZone initial = dist < overlayThreshold ? ProximityZone.Overlay : ProximityZone.Tint;
            _zoneMemory[plantId] = initial;
            return initial;
        }

        ProximityZone next = prev;

        switch (prev)
        {
            case ProximityZone.Overlay:
                if (dist > overlayThreshold + hysteresisBand) next = ProximityZone.Tint;
                break;

            case ProximityZone.Tint:
                if (dist < overlayThreshold - hysteresisBand) next = ProximityZone.Overlay;
                break;
        }

        _zoneMemory[plantId] = next;
        return next;
    }
}