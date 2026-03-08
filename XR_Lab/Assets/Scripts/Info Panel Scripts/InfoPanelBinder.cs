using System;
using System.Text;
using UnityEngine;
using TMPro;

public class InfoPanelBinder : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] private TMP_Text speciesText;
    [SerializeField] private TMP_Text idText;

    [Header("Growth")]
    [SerializeField] private TMP_Text growthText;

    [Header("Moisture")]
    [SerializeField] private TMP_Text moistureText;

    [Header("Health")]
    [SerializeField] private TMP_Text healthText;

    [Header("Pesticide")]
    [SerializeField] private TMP_Text pesticideText;

    [Header("Warning")]
    [SerializeField] private GameObject warningContainer;
    [SerializeField] private TMP_Text warningTitle;
    [SerializeField] private TMP_Text warningText;

    public void Populate(Plant plant, Row row)
    {
        if (plant == null) return;

        if (speciesText != null) speciesText.text = plant.species;
        if (idText != null)      idText.text = plant.plantId;

        PopulateGrowth(plant);
        PopulateMoisture(row);
        PopulateHealth(plant);
        PopulatePesticide(plant);
        PopulateWarnings(plant, row);
    }

    // Backward-compatible overload (no row = no moisture warning)
    public void Populate(Plant plant) => Populate(plant, null);

    private void PopulateGrowth(Plant plant)
    {
        if (growthText == null) return;

        string planted  = FormatDate(plant.plantedDate, "Unknown");
        string harvest  = FormatDate(plant.estimatedHarvestDate, "Unknown");

        growthText.text = $"Date Planted: {planted}\nMaturity: {plant.growth}%\nExpected Harvest: {harvest}";
    }

    private string FormatDate(DateData date, string fallback)
    {
        if (date == null || (date.day == 0 && date.month == 0 && date.year == 0))
            return fallback;

        return $"{date.day:D2}/{date.month:D2}/{date.year}";
    }

    private void PopulateMoisture(Row row)
    {
        if (moistureText == null) return;

        string lastWatered = row != null ? FormatDate(row.lastWateredDate, "Unknown") : "Unknown";
        string moisture    = row != null ? $"{row.groundMoisture}%" : "Unknown";

        moistureText.text = $"Last Watered: {lastWatered}\nMoisture: {moisture}";
    }

    private void PopulateHealth(Plant plant)
    {
        if (healthText == null) return;

        string status = string.IsNullOrEmpty(plant.healthStatus) ? "Unknown" : plant.healthStatus;
        healthText.text = $"Health Status: {status}";
    }

    private void PopulatePesticide(Plant plant)
    {
        if (pesticideText == null) return;

        string last = FormatDate(plant.lastPesticide, "Unknown");
        string next = FormatDate(plant.nextPesticide, "Unknown");

        pesticideText.text = $"Last Pesticide: {last}\nNext Pesticide: {next}";
    }

    private void PopulateWarnings(Plant plant, Row row)
    {
        bool isBadHealth   = string.Equals(plant.healthStatus, OverviewRules.BadHealthStatus, StringComparison.OrdinalIgnoreCase);
        bool isLowMoisture = row != null && row.groundMoisture < OverviewRules.LowMoistureThreshold;
        bool hasNoteWarn   = string.Equals(plant.notes?.noteTag, OverviewRules.WarningNoteTag, StringComparison.OrdinalIgnoreCase);

        bool anyWarning = isBadHealth || isLowMoisture || hasNoteWarn;

        if (warningContainer != null)
            warningContainer.SetActive(anyWarning);

        if (!anyWarning) return;

        // Single warning — use its specific title
        if (isBadHealth && !isLowMoisture && !hasNoteWarn)
        {
            SetWarning("Health Warning", "This plant has bad health status.");
            return;
        }

        if (isLowMoisture && !isBadHealth && !hasNoteWarn)
        {
            SetWarning("Low Moisture", $"This plant is in an area that has {row.groundMoisture}% moisture.");
            return;
        }

        if (hasNoteWarn && !isBadHealth && !isLowMoisture)
        {
            SetWarning("Warning", plant.notes.textNote);
            return;
        }

        // Multiple warnings — list them all
        var sb = new StringBuilder();
        if (isBadHealth)   sb.AppendLine("• This plant has bad health status.");
        if (isLowMoisture) sb.AppendLine($"• This plant is in an area that has {row.groundMoisture}% moisture.");
        if (hasNoteWarn)   sb.AppendLine($"• {plant.notes.textNote}");

        SetWarning("Warnings", sb.ToString().TrimEnd());
    }

    private void SetWarning(string title, string text)
    {
        if (warningTitle != null) warningTitle.text = title;
        if (warningText  != null) warningText.text  = text;
    }
}
