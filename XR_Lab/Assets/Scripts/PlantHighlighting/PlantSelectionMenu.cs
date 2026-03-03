using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Bridges PlantHighlightController to your menu UI.
/// Attach this to your menu GameObject and assign the controller in the Inspector.
///
/// Two ways to drive your UI from this script:
///
/// OPTION A — UnityEvents (no code needed, wire up in Inspector):
///   - onMenuPopulate    -> called with the eligible plant list when data is ready
///   - onHighlightChanged -> called with (plantId, bool) when a single plant changes
///   - onAllCleared      -> called when all highlights are cleared
///
/// OPTION B — Subclass (recommended for more control):
///   Create a class that extends PlantSelectionMenu and override:
///   - OnPopulateMenu(plants)                  -> instantiate your toggle prefabs
///   - OnPlantHighlightStateChanged(id, state) -> update toggle visual
///   - OnAllHighlightsCleared()                -> reset all toggles
///
/// Calling into the controller from your UI:
///   - Toggle OnClick  -> call TogglePlant(plantId, isOn)
///   - Highlight All   -> call HighlightAll()
///   - Clear All       -> call ClearAll()
/// </summary>
public class PlantSelectionMenu : MonoBehaviour
{
    [SerializeField] private PlantHighlightController highlightController;

    [Header("Unity Events — wire these to UI in the Inspector")]
    public UnityEvent<List<Plant>> onMenuPopulate;
    public UnityEvent<string, bool> onHighlightChanged;
    public UnityEvent onAllCleared;

    // ----- Unity lifecycle ----------

    private void OnEnable()
    {
        if (highlightController == null)
        {
            highlightController = PlantHighlightController.Instance;
            if (highlightController == null)
            {
                Debug.LogWarning("[PlantSelectionMenu] No PlantHighlightController found.");
                return;
            }
        }

        highlightController.OnEligiblePlantsReady += HandleEligiblePlantsReady;
        highlightController.OnHighlightChanged += HandleHighlightChanged;
        highlightController.OnAllHighlightsCleared += HandleAllCleared;

        // If controller already has data (e.g. menu opened after startup), populate immediately
        if (highlightController.EligiblePlants.Count > 0)
            HandleEligiblePlantsReady(highlightController.EligiblePlants);
    }

    private void OnDisable()
    {
        if (highlightController == null) return;
        highlightController.OnEligiblePlantsReady -= HandleEligiblePlantsReady;
        highlightController.OnHighlightChanged -= HandleHighlightChanged;
        highlightController.OnAllHighlightsCleared -= HandleAllCleared;
    }

    // --- Public methods — call these from UI button OnClick events ------

    /// <summary>Called by a toggle when the user selects or deselects a plant.</summary>
    public void TogglePlant(string plantId, bool isOn)
        => highlightController?.SetPlantHighlight(plantId, isOn);

    /// <summary>Called by a "Highlight All" button.</summary>
    public void HighlightAll()
        => highlightController?.HighlightAll();

    /// <summary>Called by a "Clear All" button.</summary>
    public void ClearAll()
        => highlightController?.ClearAllHighlights();

    // --- Internal callbacks ------

    private void HandleEligiblePlantsReady(IReadOnlyList<Plant> plants)
    {
        var list = new List<Plant>(plants);
        onMenuPopulate?.Invoke(list);
        OnPopulateMenu(list);
    }

    private void HandleHighlightChanged(string plantId, bool highlighted)
    {
        onHighlightChanged?.Invoke(plantId, highlighted);
        OnPlantHighlightStateChanged(plantId, highlighted);
    }

    private void HandleAllCleared()
    {
        onAllCleared?.Invoke();
        OnAllHighlightsCleared();
    }

    // --- Override in subclasses ------

    /// <summary>
    /// Called when the eligible plant list is ready.
    /// Use this to instantiate your toggle prefabs — one per plant in the list.
    /// Each toggle should call TogglePlant(plant.plantId, isOn) when clicked.
    /// </summary>
    protected virtual void OnPopulateMenu(List<Plant> eligiblePlants) { }

    /// <summary>Called when a single plant's highlight state changes. Update toggle visuals here.</summary>
    protected virtual void OnPlantHighlightStateChanged(string plantId, bool highlighted) { }

    /// <summary>Called when all highlights are cleared. Reset all toggle visuals here.</summary>
    protected virtual void OnAllHighlightsCleared() { }
}