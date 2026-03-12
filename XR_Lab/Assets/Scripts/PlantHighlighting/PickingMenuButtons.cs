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
    [Tooltip("Image color applied directly to a button when its species is toggled ON.")]
    [SerializeField] private Color activeNormalColor = new Color(0.078f, 0.467f, 0.827f, 1f);

    // Original Image.color values captured at Start, before we ever modify them.
    // Using Image.color directly instead of button.colors (ColorBlock) so that
    // Unity's interaction-state machine (Selected, Highlighted, etc.) cannot
    // override our highlight after a click.
    private Color _tomatoDefaultColor;
    private Color _leekDefaultColor;
    private Color _radishDefaultColor;

    private void Start()
    {
        _tomatoDefaultColor = GetImageColor(tomatoButton);
        _leekDefaultColor   = GetImageColor(leekButton);
        _radishDefaultColor = GetImageColor(radishButton);

        // Disable the Button's built-in Color Tint transition so it can't
        // fight our Image.color changes (e.g. its Selected Color overriding us).
        tomatoButton.transition = Selectable.Transition.None;
        leekButton.transition   = Selectable.Transition.None;
        radishButton.transition = Selectable.Transition.None;

        // tomatoButton, leekButton, and radishButton already call
        // ModeController.TogglePickingSpecies via their Inspector OnClick events.
        // Do NOT add AddListener here — that would double-fire and cancel the toggle.
        // clearButton calls ClearPickingHighlights via its Inspector OnClick event;
        // no AddListener needed here since PickingSelectionCleared handles the UI reset.

        modeController.PickingSpeciesToggled   += OnSpeciesToggled;
        modeController.PickingSelectionCleared += OnSelectionCleared;
        modeController.ModeChanged             += OnModeChanged;
    }

    private void OnDestroy()
    {
        if (modeController == null) return;
        modeController.PickingSpeciesToggled   -= OnSpeciesToggled;
        modeController.PickingSelectionCleared -= OnSelectionCleared;
        modeController.ModeChanged             -= OnModeChanged;
    }

    // --- Event handlers ---

    private void OnSelectionCleared()
    {
        SetHighlight(tomatoButton, _tomatoDefaultColor, false);
        SetHighlight(leekButton,   _leekDefaultColor,   false);
        SetHighlight(radishButton, _radishDefaultColor, false);
    }

    private void OnSpeciesToggled(string species, bool isActive)
    {
        switch (species.ToLowerInvariant())
        {
            case "tomato": SetHighlight(tomatoButton, _tomatoDefaultColor, isActive); break;
            case "leek":   SetHighlight(leekButton,   _leekDefaultColor,   isActive); break;
            case "radish": SetHighlight(radishButton, _radishDefaultColor, isActive); break;
        }
    }

    private void OnModeChanged(AppMode mode)
    {
        // Reset all highlights whenever the mode changes.
        SetHighlight(tomatoButton, _tomatoDefaultColor, false);
        SetHighlight(leekButton,   _leekDefaultColor,   false);
        SetHighlight(radishButton, _radishDefaultColor, false);
    }

    // --- Helpers ---

    private static Color GetImageColor(Button button)
    {
        if (button == null) return Color.white;
        Image img = button.GetComponent<Image>();
        return img != null ? img.color : Color.white;
    }

    private void SetHighlight(Button button, Color defaultColor, bool active)
    {
        if (button == null) return;
        Image img = button.GetComponent<Image>();
        if (img == null) return;
        img.color = active ? activeNormalColor : defaultColor;
    }
}
