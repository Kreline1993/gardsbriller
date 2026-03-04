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
    [SerializeField] private Color overviewLowMoistureColor = new Color(0.5f, 0f, 1f, 1f);
    [SerializeField] private Color overviewBadHealthColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color overviewWarningTagColor = new Color(1f, 0f, 0f, 1f);
    [Tooltip("Optional. When set, this prefab is spawned at each plant position instead of tinting the original plant.")]
    [SerializeField] private GameObject overviewOverlayPrefab;
    [Tooltip("When true, the original plant is hidden while the overlay is active.")]
    [SerializeField] private bool overviewHideOriginalDuringOverlay = true;
    [Tooltip("Height of the bounding box spawned over low-moisture rows.")]
    [SerializeField] private float overviewRowOverlayHeight = 1.5f;

    [Header("Weeding Mode")]
    [SerializeField] private Color weedingProtectedTint = Color.yellow;
    [SerializeField] private bool disableTouchForProtectedPlants = true;
    [Tooltip("Optional. When set, this prefab is spawned at each plant position with the tint applied instead of tinting the original plant.")]
    [SerializeField] private GameObject weedingOverlayPrefab;
    [Tooltip("When true, the original plant prefab is hidden while the overlay is active.")]
    [SerializeField] private bool hideOriginalDuringOverlay = true;

    [Header("Picking Mode")]
    [SerializeField] private Color pickingHighlightTint = new Color(1f, 0.4f, 0.8f, 1f);

    private readonly Dictionary<AppMode, IModeState> states = new Dictionary<AppMode, IModeState>();
    private IModeState currentState;

    public AppMode CurrentMode { get; private set; }
    public event Action<AppMode> ModeChanged;

    private void Awake()
    {
        if (twinDatabase == null)
            twinDatabase = FindObjectOfType<TwinDatabase>();

        if (plantVisualRegistry == null)
            plantVisualRegistry = FindObjectOfType<PlantVisualRegistry>();

        if (plantVisualRegistry == null)
        {
            TwinGenerator twinGenerator = FindObjectOfType<TwinGenerator>();
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
        states[AppMode.Overview] = new OverviewModeState(context, overviewLowMoistureColor, overviewBadHealthColor, overviewWarningTagColor, overviewOverlayPrefab, overviewHideOriginalDuringOverlay, overviewRowOverlayHeight);
        states[AppMode.PlantPicking] = new PlantPickingModeState(context, pickingHighlightTint);
        states[AppMode.Weeding] = new WeedingModeState(context, weedingProtectedTint, disableTouchForProtectedPlants, weedingOverlayPrefab, hideOriginalDuringOverlay);
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
            pickingState.ToggleSpecies(species);
        else
            Debug.LogWarning("[ModeController] TogglePickingSpecies called but not in PlantPicking mode.");
    }

    public void ClearPickingHighlights()
    {
        if (currentState is PlantPickingModeState pickingState)
            pickingState.ClearAll();
        else
            Debug.LogWarning("[ModeController] ClearPickingHighlights called but not in PlantPicking mode.");
    }
}