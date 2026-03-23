using System;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InfoPanelBinder : MonoBehaviour
{
    private enum RuleType
    {
        BadHealth,
        NoteAttached,
        RipeGrowth,
        LowMoisture
    }

    [Header("Heading")]
    [SerializeField] private Image headerBackgroundImage;
    [SerializeField] private Image[] linkedHeadingImages;
    [SerializeField] private Color defaultHeadingColor = Color.white;
    [SerializeField] private Color noteHeadingColor    = new Color(1f, 0.55f, 0f, 1f); // orange
    [SerializeField] private Color badHealthColor      = Color.red;
    [SerializeField] private Color readyHarvestColor   = Color.green;
    [SerializeField] private Color lowWaterColor       = Color.blue;

    [Header("Rule Priority")]
    [Tooltip("When multiple rules match, the first in this order wins.")]
    [SerializeField] private RuleType[] priority =
    {
        RuleType.BadHealth,
        RuleType.NoteAttached,
        RuleType.RipeGrowth,
        RuleType.LowMoisture
    };

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

    [Header("Actions")]
    [SerializeField] private Button closeButton;

    private InfoPanelSpawner _spawner;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
    }

    public void Initialize(InfoPanelSpawner spawner)
    {
        _spawner = spawner;
    }

    public void OnCloseButtonClicked()
    {
        _spawner?.ClosePanel();
    }

    public void Populate(Plant plant, Row row)
    {
        if (plant == null) return;

        if (speciesText != null) speciesText.text = plant.species;
        if (idText != null)      idText.text = plant.plantId;

        PopulateHeadingColor(plant, row);
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
        string planted = FormatDate(plant.plantedDate, "Unknown");
        string harvest = FormatDate(plant.estimatedHarvestDate, "Unknown");
        growthText.text = $"{planted}\n{plant.growth}%\n{harvest}";
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
        moistureText.text  = $"{lastWatered}\n{moisture}";
    }

    private void PopulateHealth(Plant plant)
    {
        if (healthText == null) return;
        string status   = string.IsNullOrEmpty(plant.healthStatus) ? "Unknown" : plant.healthStatus;
        healthText.text = status;
    }

    private void PopulatePesticide(Plant plant)
    {
        if (pesticideText == null) return;
        string last          = FormatDate(plant.lastPesticide, "Unknown");
        string next          = FormatDate(plant.nextPesticide, "Unknown");
        pesticideText.text   = $"{last}\n{next}";
    }

    private void PopulateHeadingColor(Plant plant, Row row)
    {
        if (TryGetHeadingColor(plant, row, out Color headingColor))
        {
            SetHeadingColorTargets(headingColor);
            return;
        }

        SetHeadingColorTargets(defaultHeadingColor);
    }

    private void SetHeadingColorTargets(Color color)
    {
        if (headerBackgroundImage != null)
            headerBackgroundImage.color = color;

        if (linkedHeadingImages == null) return;

        for (int i = 0; i < linkedHeadingImages.Length; i++)
        {
            Image target = linkedHeadingImages[i];
            if (target != null)
                target.color = color;
        }
    }

    private bool TryGetHeadingColor(Plant plant, Row row, out Color color)
    {
        RuleType[] orderedRules = (priority != null && priority.Length > 0)
            ? priority
            : new[] { RuleType.BadHealth, RuleType.NoteAttached, RuleType.RipeGrowth, RuleType.LowMoisture };

        for (int i = 0; i < orderedRules.Length; i++)
        {
            RuleType rule = orderedRules[i];
            if (!MatchesRule(plant, row, rule))
                continue;

            color = GetColorForRule(rule);
            return true;
        }

        color = default;
        return false;
    }

    private bool MatchesRule(Plant plant, Row row, RuleType rule)
    {
        switch (rule)
        {
            case RuleType.BadHealth:
                return string.Equals(plant.healthStatus, OverviewRules.BadHealthStatus, StringComparison.OrdinalIgnoreCase);

            case RuleType.NoteAttached:
                return HasWarningNote(plant);

            case RuleType.RipeGrowth:
                return plant.growth >= OverviewRules.RipeGrowthThreshold;

            case RuleType.LowMoisture:
                return row != null && row.groundMoisture < OverviewRules.LowMoistureThreshold;

            default:
                return false;
        }
    }

    private static bool HasAttachedNote(Plant plant)
    {
        if (plant?.notes == null)
            return false;

        // Unity JsonUtility may instantiate an empty NoteData for "notes": null in JSON,
        // since it cannot represent null for [Serializable] custom types. Treat that as "no note".
        string text = plant.notes.textNote;
        string tag = plant.notes.noteTag;
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(tag))
            return false;

        return true;
    }

    private static bool HasWarningNote(Plant plant)
    {
        if (plant?.notes == null)
            return false;

        string tag = plant.notes.noteTag;
        return !string.IsNullOrWhiteSpace(tag) &&
               string.Equals(tag, OverviewRules.WarningNoteTag, StringComparison.OrdinalIgnoreCase);
    }

    private Color GetColorForRule(RuleType rule)
    {
        switch (rule)
        {
            case RuleType.BadHealth:
                return badHealthColor;
            case RuleType.NoteAttached:
                return noteHeadingColor;
            case RuleType.RipeGrowth:
                return readyHarvestColor;
            case RuleType.LowMoisture:
                return lowWaterColor;
            default:
                return defaultHeadingColor;
        }
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
