using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Helper to wire collapsible section toggle buttons to OverviewPanelBinder.
/// Attach to each button (LowMoisture, BadHealth, Warning) and assign the binder + button type.
/// </summary>
[RequireComponent(typeof(Button))]
public class OverviewSectionToggleButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler
{
    public enum SectionType
    {
        LowMoisture,
        BadHealth,
        Warnings,
        ReadyForPicking
    }

    [SerializeField] private OverviewPanelBinder binder;
    [SerializeField] private SectionType sectionType;
    [SerializeField] private Button button;
    [SerializeField] private bool verboseLogs = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (binder == null)
            binder = FindFirstObjectByType<OverviewPanelBinder>();

        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] Awake | object={name} | button={(button != null)} | binder={(binder != null)} | section={sectionType}", this);
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnToggleClicked);
            if (verboseLogs)
                Debug.Log($"[OverviewSectionToggleButton] Listener attached on {name}", this);
        }

        if (binder == null)
            binder = FindFirstObjectByType<OverviewPanelBinder>();

        if (verboseLogs && binder == null)
            Debug.LogWarning($"[OverviewSectionToggleButton] Binder not found in scene for {name}.", this);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnToggleClicked);

        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] Listener removed on {name}", this);
    }

    private void OnToggleClicked()
    {
        if (binder == null)
        {
            Debug.LogWarning("[OverviewSectionToggleButton] Binder not assigned.");
            return;
        }

        switch (sectionType)
        {
            case SectionType.LowMoisture:
                binder.ToggleLowMoistureExpanded();
                break;
            case SectionType.BadHealth:
                binder.ToggleBadHealthExpanded();
                break;
            case SectionType.Warnings:
                binder.ToggleWarningsExpanded();
                break;
            case SectionType.ReadyForPicking:
                binder.ToggleRipeExpanded();
                break;
        }

        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] Toggled {sectionType}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] PointerEnter on {name}", this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] PointerDown on {name}", this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (verboseLogs)
            Debug.Log($"[OverviewSectionToggleButton] PointerClick on {name}", this);
    }

    [ContextMenu("Test Toggle")]
    private void TestToggle()
    {
        OnToggleClicked();
    }
}
