using System;
using System.Collections.Generic;
using UnityEngine;


public class ModeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TwinDatabase twinDatabase;
    [SerializeField] private PlantVisualRegistry plantVisualRegistry;

    [Header("Startup")]
    [SerializeField] private AppMode initialMode = AppMode.Default;

    [Header("Overview Mode")]
    [Tooltip("Prefab for the bounding box spawned over low-moisture rows.")]
    [SerializeField] private GameObject overviewRowOverlayPrefab;
    [Tooltip("Height of the bounding box spawned over low-moisture rows.")]
    [SerializeField] private float overviewRowOverlayHeight = 1.5f;

    [Tooltip("Icon prefab shown above / clustering plants with bad health status.")]
    [SerializeField] private GameObject overviewBadHealthIconPrefab;

    [Tooltip("Icon prefab shown above / clustering plants with a warning note tag.")]
    [SerializeField] private GameObject overviewWarningIconPrefab;

    [Tooltip("Icon prefab shown above / clustering plants with growth >= 100.")]
    [SerializeField] private GameObject overviewRipeIconPrefab;

    [Tooltip("Assign the scene GameObject that has PlantIconLODController attached. " +
             "All LOD and cluster values are edited directly on that component.")]
    [SerializeField] private PlantIconLODController overviewLODController;

    [Header("Weeding Mode")]
    [SerializeField] private Color weedingProtectedTint = Color.yellow;
    [SerializeField] private bool disableTouchForProtectedPlants = true;
    [SerializeField] private float weedingHighlightDistance = 2f;
    [Tooltip("Optional. When set, this prefab is spawned at each plant position with the tint applied instead of tinting the original plant.")]
    [SerializeField] private GameObject weedingOverlayPrefab;
    [Tooltip("When true, the original plant prefab is hidden while the overlay is active.")]
    [SerializeField] private bool hideOriginalDuringOverlay = true;

    [Header("Picking Mode")]
    [SerializeField] private Color tomatoPickingTint = new Color(1f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color carrotPickingTint = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private Color radishPickingTint = new Color(0.4f, 0.6f, 1f, 1f);
    [Tooltip("Prefab spawned over selected tomato plants within the overlay threshold distance.")]
    [SerializeField] private GameObject tomatoPickingOverlayPrefab;
    [Tooltip("Prefab spawned over selected carrot plants within the overlay threshold distance.")]
    [SerializeField] private GameObject carrotPickingOverlayPrefab;
    [Tooltip("Prefab spawned over selected radish plants within the overlay threshold distance.")]
    [SerializeField] private GameObject radishPickingOverlayPrefab;
    [Tooltip("Assign the scene GameObject that has PickingProximityController attached.")]
    [SerializeField] private PickingProximityController pickingProximityController;


    private readonly Dictionary<AppMode, IModeState> states = new Dictionary<AppMode, IModeState>();
    private IModeState currentState;

    public AppMode CurrentMode { get; private set; }
    public event Action<AppMode> ModeChanged;

    /// <summary>
    /// Fired after a species is toggled in PlantPicking mode.
    /// Args: (species, isNowActive)
    /// </summary>
    public event Action<string, bool> PickingSpeciesToggled;

    /// <summary>
    /// Fired when all picking highlights are cleared via ClearPickingHighlights().
    /// </summary>
    public event Action PickingSelectionCleared;

    private void Awake()
    {
        if (twinDatabase == null)
            twinDatabase = FindFirstObjectByType<TwinDatabase>();

        if (plantVisualRegistry == null)
            plantVisualRegistry = FindFirstObjectByType<PlantVisualRegistry>();

        if (plantVisualRegistry == null)
        {
            TwinGenerator twinGenerator = FindFirstObjectByType<TwinGenerator>();
            if (twinGenerator != null)
            {
                Debug.Log("[ModeController] PlantVisualRegistry not found. Adding to TwinGenerator.");
                plantVisualRegistry = twinGenerator.gameObject.AddComponent<PlantVisualRegistry>();
            }
            else
            {
                Debug.LogWarning("[ModeController] Could not find TwinGenerator to attach PlantVisualRegistry.");
            }
        }

        ModeContext context = new ModeContext(twinDatabase, plantVisualRegistry);

        states[AppMode.Default] = new DefaultModeState(context);
        states[AppMode.Overview] = new OverviewModeState(context,
            overviewRowOverlayPrefab,
            overviewRowOverlayHeight,
            overviewBadHealthIconPrefab,
            overviewWarningIconPrefab,
            overviewRipeIconPrefab,
            overviewLODController);
        var speciesTints = new Dictionary<string, Color>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Tomato", tomatoPickingTint },
            { "Carrot",   carrotPickingTint  },
            { "Radish", radishPickingTint }
        };
        var speciesOverlays = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase)
{
    { "Tomato", tomatoPickingOverlayPrefab },
    { "Carrot",   carrotPickingOverlayPrefab   },
    { "Radish", radishPickingOverlayPrefab }
};
        states[AppMode.PlantPicking] = new PlantPickingModeState(context, speciesTints,
            pickingProximityController, speciesOverlays);
        states[AppMode.Weeding] = new WeedingModeState(context, 
            weedingProtectedTint, 
            disableTouchForProtectedPlants, 
            pickingProximityController,
            weedingHighlightDistance,
            weedingOverlayPrefab, 
            hideOriginalDuringOverlay);
    }

    private void Start()
    {
        SwitchMode(initialMode);
    }

    private void Update()
    {
        currentState?.Tick();
    }

    public void SwitchMode(AppMode mode)
    {
        if (!states.TryGetValue(mode, out IModeState nextState))
        {
            Debug.LogWarning($"[ModeController] Mode not registered: {mode}");
            return;
        }

        currentState?.Exit();
        currentState = nextState;
        CurrentMode = mode;

        if (plantVisualRegistry != null && plantVisualRegistry.HandlesByPlantId.Count == 0)
            plantVisualRegistry.RebuildIndex();


        currentState.Enter();
        ModeChanged?.Invoke(mode);
    }

    public void SwitchModeByName(string modeName)
    {
        if (Enum.TryParse(modeName, true, out AppMode mode))
            SwitchMode(mode);
        else
            Debug.LogWarning($"[ModeController] Unknown mode name: {modeName}");
    }

    public void TogglePickingSpecies(string species)
    {
        if (currentState is PlantPickingModeState pickingState)
        {
            pickingState.ToggleSpecies(species);
            PickingSpeciesToggled?.Invoke(species, pickingState.IsSpeciesSelected(species));
        }
        else
            Debug.LogWarning("[ModeController] TogglePickingSpecies called but not in PlantPicking mode.");
    }

    /// <summary>
    /// Returns true if at least one eligible plant of the given species is currently highlighted.
    /// Safe to call from any mode (returns false if not in PlantPicking mode).
    /// </summary>
    public bool IsSpeciesSelected(string species)
    {
        return currentState is PlantPickingModeState pickingState
               && pickingState.IsSpeciesSelected(species);
    }

    public void ClearPickingHighlights()
    {
        if (currentState is PlantPickingModeState pickingState)
        {
            pickingState.ClearAll();
            PickingSelectionCleared?.Invoke();
        }
        else
            Debug.LogWarning("[ModeController] ClearPickingHighlights called but not in PlantPicking mode.");
    }
}