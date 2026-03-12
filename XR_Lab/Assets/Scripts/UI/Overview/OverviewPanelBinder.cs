using System.Collections.Generic;
using System.Text;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OverviewPanelBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OverviewPanelDataProvider dataProvider;

    [Header("Status Bar (Compact View)")]
    [Tooltip("Root GameObject for the compact status bar. Shown when collapsed.")]
    [SerializeField] private GameObject statusBarRoot;
    [SerializeField] private TMP_Text statusBarMoistureCount;
    [SerializeField] private TMP_Text statusBarHealthCount;
    [SerializeField] private TMP_Text statusBarWarningCount;
    [SerializeField] private TMP_Text statusBarRipeCount;
    [SerializeField] private TMP_Text statusBarQuickStats;
    [Tooltip("Optional background images behind each badge, tinted to match category color.")]
    [SerializeField] private Image statusBarMoistureBadge;
    [SerializeField] private Image statusBarHealthBadge;
    [SerializeField] private Image statusBarWarningBadge;
    [SerializeField] private Image statusBarRipeBadge;

    [Header("Detail Panel (Full View)")]
    [Tooltip("Root GameObject for the full detail panel. Shown when expanded.")]
    [SerializeField] private GameObject detailPanelRoot;

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

    [Header("Interaction Surface")]
    [Tooltip("The BoundsClipper on the ISDK Surface child (under ISDK_RayCanvasInteraction). " +
             "Its Size/Position are updated when toggling so the ray interaction area matches only the visible content.")]
    [SerializeField] private BoundsClipper boundsClipper;
    [Tooltip("BoundsClipper Size when showing the full detail panel (canvas pixel units). Match your canvas size.")]
    [SerializeField] private Vector3 expandedBoundsSize = new Vector3(1000, 1976, 0.01f);
    [Tooltip("BoundsClipper Size when showing the compact status bar (canvas pixel units).")]
    [SerializeField] private Vector3 collapsedBoundsSize = new Vector3(600, 100, 0.01f);
    [Tooltip("BoundsClipper Position offset when collapsed (local space). Adjust if status bar is not canvas-centered.")]
    [SerializeField] private Vector3 collapsedBoundsPosition = Vector3.zero;

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [Tooltip("When true, the panel starts in compact (status bar) mode.")]
    [SerializeField] private bool startCollapsed = true;

    private bool isExpanded;

    [Header("Expand Layout")]
    [Tooltip("Extra padding added below each expanded section's text.")]
    [SerializeField] private float expandPadding = 10f;

    private bool expandedLowMoisture = false;
    private bool expandedBadHealth = false;
    private bool expandedWarnings = false;
    private bool expandedRipe = false;

    private Color lowMoistureColor = new Color(0.5f, 0f, 1f, 1f);
    private Color badHealthColor = new Color(1f, 0.5f, 0f, 1f);
    private Color warningTagColor = new Color(1f, 0f, 0f, 1f);
    private Color ripeColor = new Color(0f, 0.8f, 0.2f, 1f);

    private OverviewPanelDataSnapshot currentSnapshot;

    // Layout bookkeeping: original positions/sizes of each Content child
    private Transform contentParent;
    private float[] baseYPositions;
    private float[] baseHeights;
    private float baseContentHeight;
    private int[] detailSectionIndex; // maps each of the 4 detail texts to a Content child index
    private bool layoutCaptured;

    // Sub-child bookkeeping: original local positions inside each section button
    private Vector2[][] baseSectionChildPositions; // [sectionSlot 0-3][childIndex]
    private RectTransform[][] sectionChildRects;   // [sectionSlot 0-3][childIndex]

    private void Awake()
    {
        if (dataProvider == null)
            dataProvider = FindFirstObjectByType<OverviewPanelDataProvider>();
    }

    private void Start()
    {
        isExpanded = !startCollapsed;
        ApplyExpandedState();
    }

    /// <summary>
    /// Toggles between the compact status bar and the full detail panel.
    /// Wire this to a Button.onClick in the Inspector.
    /// </summary>
    public void ToggleExpandCollapse()
    {
        isExpanded = !isExpanded;
        ApplyExpandedState();
    }

    /// <summary>
    /// Expands or collapses the panel from code.
    /// </summary>
    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;
        ApplyExpandedState();
    }

    public bool IsExpanded => isExpanded;

    private void ApplyExpandedState()
    {
        if (statusBarRoot != null)
            statusBarRoot.SetActive(!isExpanded);

        if (detailPanelRoot != null)
            detailPanelRoot.SetActive(isExpanded);

        UpdateBoundsClipperToActiveView();
    }

    private void UpdateBoundsClipperToActiveView()
    {
        if (boundsClipper == null)
            return;

        if (isExpanded)
        {
            boundsClipper.Position = Vector3.zero;
            boundsClipper.Size = expandedBoundsSize;
        }
        else
        {
            boundsClipper.Position = collapsedBoundsPosition;
            boundsClipper.Size = collapsedBoundsSize;
        }
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

        RenderStatusBar(snapshot);
        RenderCombined(snapshot);
        RenderSplit(snapshot);
    }

    private void RenderStatusBar(OverviewPanelDataSnapshot snapshot)
    {
        if (statusBarRoot == null)
            return;

        var summary = snapshot.summary;

        SetBadge(statusBarMoistureCount, statusBarMoistureBadge,
            summary.lowMoistureRows, lowMoistureColor);

        SetBadge(statusBarHealthCount, statusBarHealthBadge,
            summary.badHealthPlants, badHealthColor);

        SetBadge(statusBarWarningCount, statusBarWarningBadge,
            summary.warningPlants, warningTagColor);

        SetBadge(statusBarRipeCount, statusBarRipeBadge,
            summary.ripePlants, ripeColor);

        if (statusBarQuickStats != null)
        {
            statusBarQuickStats.text =
                $"Moisture: {snapshot.lowestRowMoisture}%  •  Watered: {snapshot.lastWateredDate}";
        }
    }

    private static void SetBadge(TMP_Text countText, Image badge, int count, Color categoryColor)
    {
        if (countText != null)
            countText.text = count.ToString();

        if (badge != null)
            badge.color = count > 0 ? categoryColor : Color.green;
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

        RecalculateLayout();
    }

    #region Expand Layout

    private void CaptureBaseLayout()
    {
        TMP_Text anyDetail = lowMoistureDetailsText ?? badHealthDetailsText
                             ?? warningsDetailsText ?? ripeDetailsText;
        if (anyDetail == null) return;

        // detail text → section button → Content
        contentParent = anyDetail.transform.parent.parent;
        int count = contentParent.childCount;

        baseYPositions = new float[count];
        baseHeights = new float[count];

        for (int i = 0; i < count; i++)
        {
            RectTransform child = contentParent.GetChild(i) as RectTransform;
            if (child != null)
            {
                baseYPositions[i] = child.anchoredPosition.y;
                baseHeights[i] = child.sizeDelta.y;
            }
        }

        RectTransform contentRect = contentParent as RectTransform;
        baseContentHeight = contentRect != null ? contentRect.sizeDelta.y : 0f;

        detailSectionIndex = new int[4];
        detailSectionIndex[0] = SectionIndexOf(lowMoistureDetailsText);
        detailSectionIndex[1] = SectionIndexOf(badHealthDetailsText);
        detailSectionIndex[2] = SectionIndexOf(warningsDetailsText);
        detailSectionIndex[3] = SectionIndexOf(ripeDetailsText);

        // Capture local positions of each section button's children (header, icon, details, etc.)
        baseSectionChildPositions = new Vector2[4][];
        sectionChildRects = new RectTransform[4][];

        for (int d = 0; d < 4; d++)
        {
            int idx = detailSectionIndex[d];
            if (idx < 0) continue;

            RectTransform section = contentParent.GetChild(idx) as RectTransform;
            if (section == null) continue;

            int childCount = section.childCount;
            baseSectionChildPositions[d] = new Vector2[childCount];
            sectionChildRects[d] = new RectTransform[childCount];

            for (int c = 0; c < childCount; c++)
            {
                RectTransform cr = section.GetChild(c) as RectTransform;
                sectionChildRects[d][c] = cr;
                baseSectionChildPositions[d][c] = cr != null ? cr.anchoredPosition : Vector2.zero;
            }
        }

        layoutCaptured = true;
    }

    private int SectionIndexOf(TMP_Text detail)
    {
        if (detail == null || contentParent == null) return -1;
        Transform sectionButton = detail.transform.parent;
        for (int i = 0; i < contentParent.childCount; i++)
        {
            if (contentParent.GetChild(i) == sectionButton)
                return i;
        }
        return -1;
    }

    private void RecalculateLayout()
    {
        if (!layoutCaptured) CaptureBaseLayout();
        if (!layoutCaptured) return;

        int childCount = contentParent.childCount;
        TMP_Text[] detailTexts =
        {
            lowMoistureDetailsText, badHealthDetailsText,
            warningsDetailsText, ripeDetailsText
        };

        // Determine how much extra height each section needs
        float[] extraHeight = new float[childCount];
        for (int d = 0; d < 4; d++)
        {
            int idx = detailSectionIndex[d];
            if (idx < 0 || detailTexts[d] == null) continue;

            if (!string.IsNullOrEmpty(detailTexts[d].text))
            {
                detailTexts[d].ForceMeshUpdate();
                extraHeight[idx] = detailTexts[d].preferredHeight + expandPadding;
            }
        }

        // Walk through children: restore base size, shift by accumulated expansion
        float accumulatedShift = 0f;
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = contentParent.GetChild(i) as RectTransform;
            if (child == null) continue;

            // A center-pivot RectTransform grows equally up and down.
            // Shift it down so the TOP edge stays fixed and growth is purely downward.
            float pivotCompensation = extraHeight[i] * (1f - child.pivot.y);

            Vector2 pos = child.anchoredPosition;
            pos.y = baseYPositions[i] - accumulatedShift - pivotCompensation;
            child.anchoredPosition = pos;

            Vector2 size = child.sizeDelta;
            size.y = baseHeights[i] + extraHeight[i];
            child.sizeDelta = size;

            // The button moved down, but its children (header, icon, etc.) are
            // positioned relative to the button's center, so they moved down too.
            // Push them back up so they visually stay in their original spot.
            for (int d = 0; d < 4; d++)
            {
                if (detailSectionIndex[d] != i) continue;
                if (sectionChildRects[d] == null) break;

                for (int c = 0; c < sectionChildRects[d].Length; c++)
                {
                    if (sectionChildRects[d][c] == null) continue;
                    Vector2 basePos = baseSectionChildPositions[d][c];
                    basePos.y += pivotCompensation;
                    sectionChildRects[d][c].anchoredPosition = basePos;
                }
                break;
            }

            accumulatedShift += extraHeight[i];
        }

        // Grow the Content rect so the ScrollView knows the new total height
        RectTransform contentRect = contentParent as RectTransform;
        if (contentRect != null)
        {
            Vector2 contentSize = contentRect.sizeDelta;
            contentSize.y = baseContentHeight + accumulatedShift;
            contentRect.sizeDelta = contentSize;
        }
    }

    #endregion

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
