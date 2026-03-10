using System;
using UnityEngine;
using Oculus.Interaction;

[DisallowMultipleComponent]
public sealed class PlantRuleOutlineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlantIdentity plantIdentity;
    [SerializeField] private ModeController modeController;
    [SerializeField] private InteractableUnityEventWrapper interactableEvents;

    [Header("Visual Type")]
    [SerializeField] private VisualType visualType = VisualType.QuickOutline;

    [Header("QuickOutline")]
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    [SerializeField] private float outlineWidth = 5f;

    [Header("Edge Wireframe")]
    [Tooltip("Assign PlantEdgeWireframe shader asset directly for device builds where Shader.Find may fail.")]
    [SerializeField] private Shader wireframeShader;
    [SerializeField, Min(0.5f)] private float wireframeWidth = 1.5f;
    [Tooltip("When enabled, edges are visible through the object's own faces and other scene geometry (ZTest Always). Recommended for full wireframe visibility.")]
    [SerializeField] private bool wireframeVisibleThroughObjects = true;

    [Header("Mode Behavior")]
    [Tooltip("When enabled, outline is only shown while AppMode is Overview.")]
    [SerializeField] private bool showOnlyInOverviewMode = true;

    [Header("Interaction Behavior")]
    [Tooltip("When enabled, the outline is only visible while the plant is hovered.")]
    [SerializeField] private bool showOnlyWhileHovered = false;

    [Header("Rule Colors")]
    [SerializeField] private Color badHealthColor = Color.red;
    [SerializeField] private Color warningTagColor = new Color(1f, 0.65f, 0f, 1f);
    [SerializeField] private Color ripeGrowthColor = Color.green;
    [SerializeField] private Color lowMoistureColor = Color.yellow;

    [Header("Generic Hover Color")]
    [Tooltip("Color used for hover outline when plant has no applicable rule.")]
    [SerializeField] private Color genericHoverColor = Color.white;

    [Header("Rule Priority")]
    [Tooltip("When multiple rules match, the first in this order wins.")]
    [SerializeField] private RuleType[] priority =
    {
        RuleType.BadHealth,
        RuleType.WarningTag,
        RuleType.RipeGrowth,
        RuleType.LowMoisture
    };

    [Header("Refresh")]
    [SerializeField, Min(0.1f)] private float refreshIntervalSeconds = 1f;

    private Outline outline;
    private PlantEdgeWireframeRenderer wireframe;
    private float nextRefreshTime;
    private bool isHovered;
    private bool isPanelOpen;

    private enum VisualType
    {
        QuickOutline,
        EdgeWireframe
    }

    private enum RuleType
    {
        BadHealth,
        WarningTag,
        RipeGrowth,
        LowMoisture
    }

    private void Awake()
    {
        if (plantIdentity == null)
            plantIdentity = GetComponent<PlantIdentity>();

        if (modeController == null)
            modeController = FindObjectOfType<ModeController>();

        if (interactableEvents == null)
            interactableEvents = GetComponent<InteractableUnityEventWrapper>();

        EnsureVisualComponents();
    }

    private void OnEnable()
    {
        if (modeController != null)
            modeController.ModeChanged += HandleModeChanged;

        if (interactableEvents != null)
        {
            interactableEvents.WhenHover.AddListener(OnHoverEnter);
            interactableEvents.WhenUnhover.AddListener(OnHoverExit);
        }

        RefreshNow();
    }

    private void OnDisable()
    {
        if (modeController != null)
            modeController.ModeChanged -= HandleModeChanged;

        if (interactableEvents != null)
        {
            interactableEvents.WhenHover.RemoveListener(OnHoverEnter);
            interactableEvents.WhenUnhover.RemoveListener(OnHoverExit);
        }

        if (outline != null)
            outline.enabled = false;

        if (wireframe != null)
            wireframe.SetEnabled(false);
    }

    private void Update()
    {
        // Avoid periodic refresh work when the highlight is not currently in use.
        if (!isHovered && !isPanelOpen)
            return;
        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime = Time.time + refreshIntervalSeconds;
        RefreshNow();
    }

    private void HandleModeChanged(AppMode _)
    {
        RefreshNow();
    }

    /// <summary>Called by InfoPanelSpawner to keep the outline active while the info panel is open.</summary>
    public void SetPanelOpen(bool open)
    {
        isPanelOpen = open;
        RefreshNow();
    }

    private void OnHoverEnter()
    {
        isHovered = true;
        RefreshNow();
    }

    private void OnHoverExit()
    {
        isHovered = false;
        RefreshNow();
    }

    private void RefreshNow()
    {
        EnsureVisualComponents();

        if (!HasValidVisualComponent())
            return;

        // Show visual if hovered or panel is open
        bool shouldShowVisual = isHovered || isPanelOpen;
        if (!shouldShowVisual)
        {
            DisableAllVisuals();
            return;
        }

        if (plantIdentity == null || string.IsNullOrEmpty(plantIdentity.plantId) || TwinDatabase.Instance == null)
        {
            DisableAllVisuals();
            return;
        }

        if (!TwinDatabase.Instance.TryGetPlantById(plantIdentity.plantId, out Plant plant) || plant == null)
        {
            DisableAllVisuals();
            return;
        }

        // Try to get rule color
        if (TryGetRuleColor(plant, out Color ruleColor))
        {
            // Rule applies: check if mode allows it
            if (IsRuleVisualAllowedInCurrentMode())
            {
                ApplyVisual(ruleColor);
                return;
            }
        }

        // No applicable rule (or mode doesn't allow rules): use generic color
        ApplyVisual(genericHoverColor);
    }

    private bool IsRuleVisualAllowedInCurrentMode()
    {
        // Rules are always allowed unless "show only in overview mode" is on
        if (!showOnlyInOverviewMode)
            return true;

        // "show only in overview mode" is on: require overview mode
        return modeController == null || modeController.CurrentMode == AppMode.Overview;
    }

    private bool TryGetRuleColor(Plant plant, out Color color)
    {
        RuleType[] orderedRules = (priority != null && priority.Length > 0)
            ? priority
            : new[] { RuleType.BadHealth, RuleType.WarningTag, RuleType.RipeGrowth, RuleType.LowMoisture };

        for (int i = 0; i < orderedRules.Length; i++)
        {
            RuleType rule = orderedRules[i];
            if (!MatchesRule(plant, rule))
                continue;

            color = GetColorForRule(rule);
            return true;
        }

        color = default;
        return false;
    }

    private bool MatchesRule(Plant plant, RuleType rule)
    {
        switch (rule)
        {
            case RuleType.BadHealth:
                return string.Equals(plant.healthStatus, OverviewRules.BadHealthStatus, StringComparison.OrdinalIgnoreCase);

            case RuleType.WarningTag:
                return plant.notes != null
                    && string.Equals(plant.notes.noteTag, OverviewRules.WarningNoteTag, StringComparison.OrdinalIgnoreCase);

            case RuleType.RipeGrowth:
                return plant.growth >= PlantHighlightState.GrowthThreshold;

            case RuleType.LowMoisture:
                Row row = TwinDatabase.Instance.GetRowForPlant(plant.plantId);
                return row != null && row.groundMoisture < OverviewRules.LowMoistureThreshold;

            default:
                return false;
        }
    }

    private Color GetColorForRule(RuleType rule)
    {
        switch (rule)
        {
            case RuleType.BadHealth:
                return badHealthColor;
            case RuleType.WarningTag:
                return warningTagColor;
            case RuleType.RipeGrowth:
                return ripeGrowthColor;
            case RuleType.LowMoisture:
                return lowMoistureColor;
            default:
                return Color.white;
        }
    }

    private void EnsureVisualComponents()
    {
        if (visualType == VisualType.QuickOutline)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

            if (outline != null)
            {
                outline.OutlineMode = outlineMode;
                outline.OutlineWidth = outlineWidth;
            }

            wireframe = GetComponent<PlantEdgeWireframeRenderer>();
            return;
        }

        wireframe = GetComponent<PlantEdgeWireframeRenderer>();
        if (wireframe == null)
            wireframe = gameObject.AddComponent<PlantEdgeWireframeRenderer>();

        if (wireframe != null)
        {
            wireframe.WireframeShader = wireframeShader;
            wireframe.EdgeWidth = wireframeWidth;
            wireframe.VisibleThroughObjects = wireframeVisibleThroughObjects;
        }

        outline = GetComponent<Outline>();
    }

    private bool HasValidVisualComponent()
    {
        return visualType == VisualType.QuickOutline
            ? outline != null
            : wireframe != null;
    }

    private void DisableAllVisuals()
    {
        if (outline != null)
            outline.enabled = false;

        if (wireframe != null)
            wireframe.SetEnabled(false);
    }

    private void ApplyVisual(Color color)
    {
        if (visualType == VisualType.QuickOutline)
        {
            if (outline == null)
                return;

            outline.OutlineMode = outlineMode;
            outline.OutlineWidth = outlineWidth;
            outline.OutlineColor = color;
            outline.enabled = true;

            if (wireframe != null)
                wireframe.SetEnabled(false);
            return;
        }

        if (wireframe == null)
            return;

        wireframe.EdgeColor = color;
        wireframe.EdgeWidth = wireframeWidth;
        wireframe.VisibleThroughObjects = wireframeVisibleThroughObjects;
        wireframe.SetEnabled(true);

        if (outline != null)
            outline.enabled = false;
    }
}