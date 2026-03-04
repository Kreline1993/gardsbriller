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

    [Header("Weeding Mode")]
    [SerializeField] private Color weedingProtectedTint = Color.yellow;
    [SerializeField] private bool disableTouchForProtectedPlants = true;

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

        ModeContext context = new ModeContext(
            twinDatabase,
            plantVisualRegistry,
            weedingProtectedTint,
            disableTouchForProtectedPlants,
            pickingHighlightTint
        );

        states[AppMode.Default] = new DefaultModeState(context);
        states[AppMode.Overview] = new OverviewModeState(context);
        states[AppMode.PlantPicking] = new PlantPickingModeState(context);
        states[AppMode.Weeding] = new WeedingModeState(context);
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

    // ----------  Mode shortcuts ----------

    public void SwitchToDefault() => SwitchMode(AppMode.Default);
    public void SwitchToOverview() => SwitchMode(AppMode.Overview);
    public void SwitchToPlantPicking() => SwitchMode(AppMode.PlantPicking);
    public void SwitchToWeeding() => SwitchMode(AppMode.Weeding);

    //---------- Picking mode controls ----------

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