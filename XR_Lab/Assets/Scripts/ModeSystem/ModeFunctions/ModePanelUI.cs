using UnityEngine;
using UnityEngine.UI;

public class ModePanelUI : MonoBehaviour
{
    [SerializeField] private ModeController modeController;
    
    [SerializeField] private Button defaultButton;
    [SerializeField] private Button overviewButton;
    [SerializeField] private Button plantPickingButton;
    [SerializeField] private Button weedingButton;
    
    [SerializeField] private Color activeButtonColor = Color.green;
    [SerializeField] private Color inactiveButtonColor = Color.gray;
    
    
    [SerializeField] private bool autoClosePanelAfterModeSelect = true;
    
    private void Start()
    {
        if (modeController == null)
            modeController = FindObjectOfType<ModeController>();

        if (modeController == null)
        {
            Debug.LogError($"[ModePanelUI] No ModeController found in scene. Disabling {gameObject.name}.", this);
            gameObject.SetActive(false);
            return;
        }

        UpdateButtonColors(modeController.CurrentMode);
        
        // Subscribe to mode changes
        modeController.ModeChanged += OnModeChanged;
        
        // Wire up button clicks
        defaultButton?.onClick.AddListener(() => { modeController.SwitchToDefault(); ClosePanelAfterModeSelect(); });
        overviewButton?.onClick.AddListener(() => { modeController.SwitchToOverview(); ClosePanelAfterModeSelect(); });
        plantPickingButton?.onClick.AddListener(() => { modeController.SwitchToPlantPicking(); ClosePanelAfterModeSelect(); });
        weedingButton?.onClick.AddListener(() => { modeController.SwitchToWeeding(); ClosePanelAfterModeSelect(); });
    }
    
    private void OnModeChanged(AppMode newMode)
    {
        UpdateButtonColors(newMode);
    }
    
    private void UpdateButtonColors(AppMode currentMode)
    {
        // Reset all buttons to inactive
        SetButtonColor(defaultButton, inactiveButtonColor);
        SetButtonColor(overviewButton, inactiveButtonColor);
        SetButtonColor(plantPickingButton, inactiveButtonColor);
        SetButtonColor(weedingButton, inactiveButtonColor);
        
        // Highlight the active button
        switch (currentMode)
        {
            case AppMode.Default:
                SetButtonColor(defaultButton, activeButtonColor);
                break;
            case AppMode.Overview:
                SetButtonColor(overviewButton, activeButtonColor);
                break;
            case AppMode.PlantPicking:
                SetButtonColor(plantPickingButton, activeButtonColor);
                break;
            case AppMode.Weeding:
                SetButtonColor(weedingButton, activeButtonColor);
                break;
        }
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }
    
    
    /// <summary>
    /// Called after a mode is selected to close the panel automatically.
    /// </summary>
    private void ClosePanelAfterModeSelect()
    {
        if (autoClosePanelAfterModeSelect)
            gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (modeController != null)
            modeController.ModeChanged -= OnModeChanged;
    }
}