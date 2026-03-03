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

        // If registry still not found, try to find TwinGenerator and add registry to it
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
            disableTouchForProtectedPlants
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
    public void SwitchToDefault()
    {
        Debug.Log("[ModeController] Switching to Default mode.");
        SwitchMode(AppMode.Default);
    }

    public void SwitchToOverview()
    {
        Debug.Log("[ModeController] Switching to Overview mode.");
        SwitchMode(AppMode.Overview);
    }

    public void SwitchToPlantPicking()
    {
        Debug.Log("[ModeController] Switching to Plant Picking mode.");
        SwitchMode(AppMode.PlantPicking);
    }

    public void SwitchToWeeding()
    {
        Debug.Log("[ModeController] Switching to Weeding mode.");
        SwitchMode(AppMode.Weeding);
    }
}
