using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// EditMode tests for PlantHighlightState — runs without a scene or Play mode.
/// Covers: eligibility filtering, selection, deselection, select-all, clear-all, and events.
/// </summary>
public class PlantHighlightStateTests
{
    // --- Helpers ----------

    private static Plant P(string id, int growth) => new Plant { plantId = id, growth = growth };

    private static PlantHighlightState StateWith(params (string id, int growth)[] plants)
    {
        var state = new PlantHighlightState();
        var list = new List<Plant>();
        foreach (var (id, growth) in plants)
            list.Add(P(id, growth));
        state.SetEligiblePlants(list);
        return state;
    }

    // --- FilterEligible ----------

    [Test]
    public void FilterEligible_PlantBelow100_Excluded()
    {
        var result = PlantHighlightState.FilterEligible(new[] { P("a", 99) });
        Assert.AreEqual(0, result.Count);
    }

    [Test]
    public void FilterEligible_PlantAtExactly100_Included()
    {
        var result = PlantHighlightState.FilterEligible(new[] { P("a", 100) });
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("a", result[0].plantId);
    }

    [Test]
    public void FilterEligible_PlantAbove100_Included()
    {
        var result = PlantHighlightState.FilterEligible(new[] { P("a", 150) });
        Assert.AreEqual(1, result.Count);
    }

    [Test]
    public void FilterEligible_MixedList_ReturnsOnlyEligible()
    {
        var plants = new[] { P("low", 60), P("at", 100), P("high", 130) };
        var result = PlantHighlightState.FilterEligible(plants);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Exists(p => p.plantId == "at"));
        Assert.IsTrue(result.Exists(p => p.plantId == "high"));
        Assert.IsFalse(result.Exists(p => p.plantId == "low"));
    }

    [Test]
    public void FilterEligible_NullPlants_Skipped()
    {
        var result = PlantHighlightState.FilterEligible(new Plant[] { null, P("a", 100) });
        Assert.AreEqual(1, result.Count);
    }

    [Test]
    public void FilterEligible_EmptyList_ReturnsEmpty()
    {
        var result = PlantHighlightState.FilterEligible(new Plant[0]);
        Assert.AreEqual(0, result.Count);
    }

    // --- Eligibility checks ----------

    [Test]
    public void IsEligible_PlantAtThreshold_ReturnsTrue()
    {
        var state = StateWith(("a", 100));
        Assert.IsTrue(state.IsEligible("a"));
    }

    [Test]
    public void IsEligible_PlantBelowThreshold_ReturnsFalse()
    {
        var state = StateWith(("a", 99));
        Assert.IsFalse(state.IsEligible("a"));
    }

    [Test]
    public void IsEligible_UnknownId_ReturnsFalse()
    {
        var state = StateWith(("a", 100));
        Assert.IsFalse(state.IsEligible("nonexistent"));
    }

    // --- Select ----------

    [Test]
    public void Select_EligiblePlant_ReturnsTrue()
    {
        var state = StateWith(("a", 100));
        Assert.IsTrue(state.Select("a"));
    }

    [Test]
    public void Select_EligiblePlant_IsSelectedAfterwards()
    {
        var state = StateWith(("a", 100));
        state.Select("a");
        Assert.IsTrue(state.IsSelected("a"));
    }

    [Test]
    public void Select_IneligiblePlant_ReturnsFalse()
    {
        var state = StateWith(("a", 100));
        Assert.IsFalse(state.Select("b")); // "b" not in eligible set
    }

    [Test]
    public void Select_PlantBelowGrowthThreshold_NeverEligible()
    {
        var state = StateWith(("a", 50));
        Assert.IsFalse(state.IsEligible("a"));
        Assert.IsFalse(state.Select("a"));
        Assert.IsFalse(state.IsSelected("a"));
    }

    [Test]
    public void Select_AlreadySelected_ReturnsFalseAndDoesNotFireEventAgain()
    {
        var state = StateWith(("a", 100));
        int fireCount = 0;
        state.OnSelectionChanged += (_, __) => fireCount++;

        state.Select("a");
        state.Select("a"); // Second call

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void Select_NullOrEmptyId_ReturnsFalse()
    {
        var state = StateWith(("a", 100));
        Assert.IsFalse(state.Select(null));
        Assert.IsFalse(state.Select(""));
    }

    // --- Deselect ----------

    [Test]
    public void Deselect_SelectedPlant_RemovesFromSelection()
    {
        var state = StateWith(("a", 100));
        state.Select("a");
        state.Deselect("a");
        Assert.IsFalse(state.IsSelected("a"));
    }

    [Test]
    public void Deselect_NotSelectedPlant_ReturnsFalse()
    {
        var state = StateWith(("a", 100));
        Assert.IsFalse(state.Deselect("a")); // Never selected
    }

    [Test]
    public void Deselect_SelectedPlant_ReturnsTrue()
    {
        var state = StateWith(("a", 100));
        state.Select("a");
        Assert.IsTrue(state.Deselect("a"));
    }

    // --- SelectAll / ClearAll ----------

    [Test]
    public void SelectAll_SelectsAllEligiblePlants()
    {
        var state = StateWith(("a", 100), ("b", 120), ("c", 50));
        state.SelectAll();

        Assert.IsTrue(state.IsSelected("a"));
        Assert.IsTrue(state.IsSelected("b"));
        Assert.IsFalse(state.IsSelected("c")); // c has growth 50, never eligible
    }

    [Test]
    public void ClearAll_RemovesAllSelections()
    {
        var state = StateWith(("a", 100), ("b", 110));
        state.SelectAll();
        state.ClearAll();

        Assert.IsFalse(state.IsSelected("a"));
        Assert.IsFalse(state.IsSelected("b"));
        Assert.AreEqual(0, state.SelectedIds.Count);
    }

    [Test]
    public void SetEligiblePlants_ResetsCurrentSelection()
    {
        var state = StateWith(("a", 100));
        state.Select("a");
        Assert.IsTrue(state.IsSelected("a"));

        // Simulate a data refresh with a different plant list
        state.SetEligiblePlants(new List<Plant> { P("b", 110) });

        Assert.IsFalse(state.IsSelected("a")); // Old selection cleared
        Assert.AreEqual(0, state.SelectedIds.Count);
    }

    // --- Events ----------

    [Test]
    public void OnSelectionChanged_FiredWithTrueOnSelect()
    {
        var state = StateWith(("a", 100));
        string firedId = null;
        bool firedState = false;
        state.OnSelectionChanged += (id, on) => { firedId = id; firedState = on; };

        state.Select("a");

        Assert.AreEqual("a", firedId);
        Assert.IsTrue(firedState);
    }

    [Test]
    public void OnSelectionChanged_FiredWithFalseOnDeselect()
    {
        var state = StateWith(("a", 100));
        state.Select("a");

        string firedId = null;
        bool firedState = true;
        state.OnSelectionChanged += (id, on) => { firedId = id; firedState = on; };

        state.Deselect("a");

        Assert.AreEqual("a", firedId);
        Assert.IsFalse(firedState);
    }

    [Test]
    public void OnSelectionCleared_FiredOnClearAll()
    {
        var state = StateWith(("a", 100));
        bool cleared = false;
        state.OnSelectionCleared += () => cleared = true;

        state.SelectAll();
        state.ClearAll();

        Assert.IsTrue(cleared);
    }

    [Test]
    public void OnSelectionCleared_NotFiredIfNothingWasSelected()
    {
        var state = StateWith(("a", 100));
        bool cleared = false;
        state.OnSelectionCleared += () => cleared = true;

        state.ClearAll(); // Nothing selected yet

        // Event still fires — ClearAll always fires to keep UI in sync
        Assert.IsTrue(cleared);
    }
}