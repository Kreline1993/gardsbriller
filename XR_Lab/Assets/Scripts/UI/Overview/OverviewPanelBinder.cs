using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class OverviewPanelBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OverviewPanelDataProvider dataProvider;

    [Header("Combined Display (Optional)")]
    [SerializeField] private TMP_Text mainText;

    [Header("Split Display (Optional)")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text lowMoistureHeaderText;
    [SerializeField] private TMP_Text lowMoistureDetailsText;
    [SerializeField] private TMP_Text badHealthHeaderText;
    [SerializeField] private TMP_Text badHealthDetailsText;
    [SerializeField] private TMP_Text warningsHeaderText;
    [SerializeField] private TMP_Text warningsDetailsText;
    [SerializeField] private TMP_Text ripeHeaderText;
    [SerializeField] private TMP_Text ripeDetailsText;

    [Header("Simple Fields (Optional)")]
    [SerializeField] private TMP_Text nextPesticidesText;
    [SerializeField] private TMP_Text lastPesticideText;
    [SerializeField] private TMP_Text lastWateredText;
    [SerializeField] private TMP_Text lowestMoistureText;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;

    // Collapsible section state
    private bool expandedLowMoisture = false;
    private bool expandedBadHealth = false;
    private bool expandedWarnings = false;
    private bool expandedRipe = false;

    // Rule colors provided by ModeController
    private Color lowMoistureColor = new Color(0.5f, 0f, 1f, 1f);
    private Color badHealthColor = new Color(1f, 0.5f, 0f, 1f);
    private Color warningTagColor = new Color(1f, 0f, 0f, 1f);
    private Color ripeColor = new Color(0f, 0.8f, 0.2f, 1f);

    private OverviewPanelDataSnapshot currentSnapshot;

    private void Awake()
    {
        if (dataProvider == null)
            dataProvider = FindObjectOfType<OverviewPanelDataProvider>();
    }

    /// <summary>
    /// Sets the colors to use for each rule category in the overview display.
    /// </summary>
    public void SetRuleColors(Color lowMoisture, Color badHealth, Color warningTag, Color ripe = default)
    {
        lowMoistureColor = lowMoisture;
        badHealthColor = badHealth;
        warningTagColor = warningTag;
        if (ripe != default)
            ripeColor = ripe;

        if (currentSnapshot != null)
            RenderAll(currentSnapshot);
    }

    private void OnEnable()
    {
        if (dataProvider != null)
            dataProvider.DataUpdated += HandleDataUpdated;

        if (refreshOnEnable && dataProvider != null)
        {
            dataProvider.RefreshNow();
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

        currentSnapshot = snapshot;
        RenderAll(snapshot);
    }

    public void ToggleLowMoistureExpanded()
    {
        expandedLowMoisture = !expandedLowMoisture;
        if (currentSnapshot != null)
            RenderAll(currentSnapshot);
    }

    public void ToggleBadHealthExpanded()
    {
        expandedBadHealth = !expandedBadHealth;
        if (currentSnapshot != null)
            RenderAll(currentSnapshot);
    }

    public void ToggleWarningsExpanded()
    {
        expandedWarnings = !expandedWarnings;
        if (currentSnapshot != null)
            RenderAll(currentSnapshot);
    }

    public void ToggleRipeExpanded()
    {
        expandedRipe = !expandedRipe;
        if (currentSnapshot != null)
            RenderAll(currentSnapshot);
    }

    private void RenderAll(OverviewPanelDataSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        RenderCombined(snapshot);
        RenderSplit(snapshot);
    }

    private void RenderCombined(OverviewPanelDataSnapshot snapshot)
    {
        if (mainText == null)
            return;

        var sb = new StringBuilder();

        sb.AppendLine(BuildSummaryText(snapshot.summary));
        sb.AppendLine();

        sb.AppendLine(BuildLowMoistureHeader(snapshot.summary.lowMoistureRows));
        if (expandedLowMoisture)
            AppendRowDetails(sb, snapshot.lowMoistureRows);
        sb.AppendLine();

        sb.AppendLine(BuildBadHealthHeader(snapshot.summary.badHealthPlants));
        if (expandedBadHealth)
            AppendPlantDetails(sb, snapshot.badHealthPlants);
        sb.AppendLine();

        sb.AppendLine(BuildWarningsHeader(snapshot.summary.warningPlants));
        if (expandedWarnings)
            AppendPlantDetails(sb, snapshot.warningPlants);
        sb.AppendLine();

        sb.AppendLine(BuildRipeHeader(snapshot.summary.ripePlants));
        if (expandedRipe)
            AppendPlantDetails(sb, snapshot.ripePlants);

        mainText.text = sb.ToString();
    }

    private void RenderSplit(OverviewPanelDataSnapshot snapshot)
    {
        if (summaryText != null)
            summaryText.text = BuildSummaryText(snapshot.summary);

        if (lowMoistureHeaderText != null)
            lowMoistureHeaderText.text = BuildLowMoistureHeader(snapshot.summary.lowMoistureRows);

        if (lowMoistureDetailsText != null)
            lowMoistureDetailsText.text = expandedLowMoisture ? BuildRowDetails(snapshot.lowMoistureRows) : string.Empty;

        if (badHealthHeaderText != null)
            badHealthHeaderText.text = BuildBadHealthHeader(snapshot.summary.badHealthPlants);

        if (badHealthDetailsText != null)
            badHealthDetailsText.text = expandedBadHealth ? BuildPlantDetails(snapshot.badHealthPlants) : string.Empty;

        if (warningsHeaderText != null)
            warningsHeaderText.text = BuildWarningsHeader(snapshot.summary.warningPlants);

        if (warningsDetailsText != null)
            warningsDetailsText.text = expandedWarnings ? BuildPlantDetails(snapshot.warningPlants) : string.Empty;

        if (ripeHeaderText != null)
            ripeHeaderText.text = BuildRipeHeader(snapshot.summary.ripePlants);

        if (ripeDetailsText != null)
            ripeDetailsText.text = expandedRipe ? BuildPlantDetails(snapshot.ripePlants) : string.Empty;

        if (nextPesticidesText != null)
            nextPesticidesText.text = $"{snapshot.nextPesticidesDate}";

        if (lastPesticideText != null)
            lastPesticideText.text = $"{snapshot.lastPesticideDate}";

        if (lastWateredText != null)
            lastWateredText.text = $"{snapshot.lastWateredDate}";

        if (lowestMoistureText != null)
            lowestMoistureText.text = $"{snapshot.lowestRowMoisture}%";
    }

    private static string BuildSummaryText(OverviewSummarySectionData summary)
    {
        if (summary == null)
            return "Overview Status";

        return "<b>Overview Status</b>\n" +
               $"Total: {summary.totalRows} rows, {summary.totalPlants} plants";
    }

    private string BuildLowMoistureHeader(int count)
    {
        string arrow = expandedLowMoisture ? "▼" : "▶";
        string colorHex = ColorUtility.ToHtmlStringRGB(lowMoistureColor);
        return count == 0
            ? $"{arrow} <color=green>[Low Moisture] Rows clear</color>"
            : $"{arrow} <color=#{colorHex}>{count} [Low Moisture] Rows need attention</color>";
    }

    private string BuildBadHealthHeader(int count)
    {
        string arrow = expandedBadHealth ? "▼" : "▶";
        string colorHex = ColorUtility.ToHtmlStringRGB(badHealthColor);
        return count == 0
            ? $"{arrow} <color=green>[Bad Health] Plants clear</color>"
            : $"{arrow} <color=#{colorHex}>{count} [Bad Health] Plants need attention</color>";
    }

    private string BuildWarningsHeader(int count)
    {
        string arrow = expandedWarnings ? "▼" : "▶";
        string colorHex = ColorUtility.ToHtmlStringRGB(warningTagColor);
        return count == 0
            ? $"{arrow} <color=green>[Warning] Plants clear</color>"
            : $"{arrow} <color=#{colorHex}>{count} [Warning] Plants need attention</color>";
    }

    private string BuildRipeHeader(int count)
    {
        string arrow = expandedRipe ? "▼" : "▶";
        string colorHex = ColorUtility.ToHtmlStringRGB(ripeColor);
        return count == 0
            ? $"{arrow} <color=green>[Ready for Picking] No plants ready</color>"
            : $"{arrow} <color=#{colorHex}>{count} [Ready for Picking] Plants ready to harvest</color>";
    }

    private static string BuildRowDetails(List<OverviewRowSectionData> rows)
    {
        var sb = new StringBuilder();
        AppendRowDetails(sb, rows);
        return sb.ToString();
    }

    private static void AppendRowDetails(StringBuilder sb, List<OverviewRowSectionData> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            sb.AppendLine("  • No low-moisture rows.");
            return;
        }

        foreach (OverviewRowSectionData row in rows)
        {
            sb.AppendLine($"  • {row.rowId} (Moisture: {row.groundMoisture}%, Plants: {row.plantCount})");
        }
    }

    private static string BuildPlantDetails(List<OverviewPlantSectionData> plants)
    {
        var sb = new StringBuilder();
        AppendPlantDetails(sb, plants);
        return sb.ToString();
    }

    private static void AppendPlantDetails(StringBuilder sb, List<OverviewPlantSectionData> plants)
    {
        if (plants == null || plants.Count == 0)
        {
            sb.AppendLine("  • No plants in this section.");
            return;
        }

        foreach (OverviewPlantSectionData plant in plants)
        {
            sb.AppendLine($"  • {plant.plantId} • {plant.species} (Row: {plant.rowId}, Growth: {plant.growth}%)");
        }
    }
}
