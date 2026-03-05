using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class OverviewPanelBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OverviewPanelDataProvider dataProvider;

    [Header("Summary Section")]
    [SerializeField] private TMP_Text summaryText;

    [Header("Rows Section")]
    [SerializeField] private TMP_Text lowMoistureRowsText;

    [Header("Bad Health Section")]
    [SerializeField] private TMP_Text badHealthPlantsText;

    [Header("Warning Section")]
    [SerializeField] private TMP_Text warningPlantsText;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;

    private void Awake()
    {
        if (dataProvider == null)
            dataProvider = FindObjectOfType<OverviewPanelDataProvider>();
    }

    private void OnEnable()
    {
        if (dataProvider != null)
            dataProvider.DataUpdated += HandleDataUpdated;

        if (refreshOnEnable && dataProvider != null)
        {
            dataProvider.RefreshNow();
            HandleDataUpdated(dataProvider.CurrentData);
        }
    }

    private void OnDisable()
    {
        if (dataProvider != null)
            dataProvider.DataUpdated -= HandleDataUpdated;
    }

    private void HandleDataUpdated(OverviewPanelDataSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        RenderSummary(snapshot.summary);
        RenderRows(snapshot.lowMoistureRows);
        RenderBadHealth(snapshot.badHealthPlants);
        RenderWarnings(snapshot.warningPlants);
    }

    private void RenderSummary(OverviewSummarySectionData summary)
    {
        if (summaryText == null || summary == null)
            return;

        summaryText.text =
            "Overview\n" +
            $"Rows: {summary.totalRows}\n" +
            $"Plants: {summary.totalPlants}\n" +
            $"Low moisture rows: {summary.lowMoistureRows}\n" +
            $"Bad health plants: {summary.badHealthPlants}\n" +
            $"Warning plants: {summary.warningPlants}";
    }

    private void RenderRows(List<OverviewRowSectionData> rows)
    {
        if (lowMoistureRowsText == null)
            return;

        if (rows == null || rows.Count == 0)
        {
            lowMoistureRowsText.text = "No low-moisture rows.";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            OverviewRowSectionData row = rows[i];
            sb.Append($"{row.rowId} | Moisture: {row.groundMoisture} | Plants: {row.plantCount}");
            if (i < rows.Count - 1)
                sb.AppendLine();
        }

        lowMoistureRowsText.text = sb.ToString();
    }

    private void RenderBadHealth(List<OverviewPlantSectionData> plants)
    {
        if (badHealthPlantsText == null)
            return;

        if (plants == null || plants.Count == 0)
        {
            badHealthPlantsText.text = "No bad-health plants.";
            return;
        }

        badHealthPlantsText.text = BuildPlantLines(plants);
    }

    private void RenderWarnings(List<OverviewPlantSectionData> plants)
    {
        if (warningPlantsText == null)
            return;

        if (plants == null || plants.Count == 0)
        {
            warningPlantsText.text = "No warning-tag plants.";
            return;
        }

        warningPlantsText.text = BuildPlantLines(plants);
    }

    private static string BuildPlantLines(List<OverviewPlantSectionData> plants)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < plants.Count; i++)
        {
            OverviewPlantSectionData plant = plants[i];
            sb.Append($"{plant.plantId} | {plant.species} | Row: {plant.rowId} | Health: {plant.healthStatus} | Tag: {plant.noteTag}");
            if (i < plants.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }
}
