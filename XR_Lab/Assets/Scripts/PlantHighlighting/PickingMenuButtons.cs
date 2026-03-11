using UnityEngine;
using UnityEngine.UI;

public class PickingMenuButtons : MonoBehaviour
{
    [SerializeField] private ModeController modeController;

    [Header("Buttons")]
    [SerializeField] private Button tomatoButton;
    [SerializeField] private Button leekButton;
    [SerializeField] private Button radishButton;
    [SerializeField] private Button clearButton;

    [Header("Highlight Color")]
    [Tooltip("Normal color applied to a button when its species is toggled ON.")]
    [SerializeField] private Color activeNormalColor = new Color(1f, 0.4f, 0.8f, 1f);

    // Original color blocks, captured at start so we can restore them.
    private ColorBlock _tomatoDefault;
    private ColorBlock _leekDefault;
    private ColorBlock _radishDefault;

    private void Start()
    {
        // Cache default color blocks before we ever touch them.
        _tomatoDefault = tomatoButton.colors;
        _leekDefault   = leekButton.colors;
        _radishDefault = radishButton.colors;

        // tomatoButton, leekButton, and radishButton already call
        // ModeController.TogglePickingSpecies via their Inspector OnClick events.
        // Do NOT add AddListener here — that would double-fire and cancel the toggle.
        // clearButton has no Inspector event, so wire it here.
        clearButton.onClick.AddListener(() => modeController.ClearPickingHighlights());

        // Subscribe to state-change events for visual feedback.
        modeController.PickingSpeciesToggled += OnSpeciesToggled;
        modeController.ModeChanged           += OnModeChanged;
    }

    private void OnDestroy()
    {
        if (modeController == null) return;
        modeController.PickingSpeciesToggled -= OnSpeciesToggled;
        modeController.ModeChanged           -= OnModeChanged;
    }

    // --- Event handlers ---

    private void OnSpeciesToggled(string species, bool isActive)
    {
        switch (species.ToLowerInvariant())
        {
            case "tomato": SetHighlight(tomatoButton, _tomatoDefault, isActive); break;
            case "leek":   SetHighlight(leekButton,   _leekDefault,   isActive); break;
            case "radish": SetHighlight(radishButton,  _radishDefault, isActive); break;
        }
    }

    private void OnModeChanged(AppMode mode)
    {
        // Reset all highlights whenever we leave (or re-enter) PlantPicking mode.
        SetHighlight(tomatoButton, _tomatoDefault, false);
        SetHighlight(leekButton,   _leekDefault,   false);
        SetHighlight(radishButton,  _radishDefault, false);
    }

    // --- Helpers ---

    private void SetHighlight(Button button, ColorBlock defaultColors, bool active)
    {
        if (active)
        {
            ColorBlock cb = defaultColors;
            cb.normalColor     = activeNormalColor;
            cb.selectedColor   = activeNormalColor;   // visible while button is focused after click
            cb.highlightedColor = activeNormalColor;  // visible while pointer hovers
            button.colors = cb;
        }
        else
        {
            button.colors = defaultColors;
        }
    }
}