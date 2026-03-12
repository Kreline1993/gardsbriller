using UnityEngine;

/// <summary>
/// Attach to a mode-specific panel. Opens when the target mode is entered,
/// closes when any other mode is entered.
/// Replaces CloseOnModeChange.
/// </summary>
public class ModePanelVisibility : MonoBehaviour
{
    [SerializeField] private ModeController modeController;
    [SerializeField] private AppMode targetMode;

    private void Start()
    {
        if (modeController == null)
            modeController = FindFirstObjectByType<ModeController>();

        if (modeController == null)
        {
            Debug.LogWarning($"[ModePanelVisibility] No ModeController found. Disabling {gameObject.name}.", this);
            enabled = false;
            return;
        }

        modeController.ModeChanged += OnModeChanged;

        // Sync to the current mode immediately on startup.
        OnModeChanged(modeController.CurrentMode);
    }

    private void OnDestroy()
    {
        if (modeController != null)
            modeController.ModeChanged -= OnModeChanged;
    }

    private void OnModeChanged(AppMode mode)
    {
        gameObject.SetActive(mode == targetMode);
    }
}
