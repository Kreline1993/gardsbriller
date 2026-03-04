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

    private void Start()
    {
        tomatoButton.onClick.AddListener(() => modeController.TogglePickingSpecies("Tomato"));
        leekButton.onClick.AddListener(() => modeController.TogglePickingSpecies("Leek"));
        radishButton.onClick.AddListener(() => modeController.TogglePickingSpecies("Radish"));
        clearButton.onClick.AddListener(() => modeController.ClearPickingHighlights());
    }
}