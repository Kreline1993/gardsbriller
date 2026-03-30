using UnityEngine;

/// <summary>
/// Generic panel toggle script that can be reused on any panel or UI object.
/// Simply attach this to any GameObject and configure which panel to toggle.
/// </summary>
public class PanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;
    private bool isPanelActive = true;

    private void Start()
    {
        if (targetPanel == null)
            targetPanel = gameObject;

        isPanelActive = targetPanel.activeSelf;
    }

    /// <summary>
    /// Toggles the visibility of the target panel.
    /// Reads actual state to stay in sync when other scripts (ModePanelVisibility, etc.) change visibility.
    /// </summary>
    public void TogglePanel()
    {
        if (targetPanel != null)
        {
            isPanelActive = !targetPanel.activeSelf;
            targetPanel.SetActive(isPanelActive);
        }
    }

    /// <summary>
    /// Shows the target panel.
    /// </summary>
    public void ShowPanel()
    {
        if (targetPanel != null)
        {
            isPanelActive = true;
            targetPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the target panel.
    /// </summary>
    public void HidePanel()
    {
        if (targetPanel != null)
        {
            isPanelActive = false;
            targetPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Gets the current state of the panel.
    /// </summary>
    public bool IsPanelActive => isPanelActive;
}
