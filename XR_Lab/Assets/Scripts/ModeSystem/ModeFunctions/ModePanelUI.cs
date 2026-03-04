using UnityEngine;
using UnityEngine.UI;

public class ModePanelUI : MonoBehaviour
{
    [System.Serializable]
    public struct ModeButtonEntry
    {
        public AppMode mode;
        public Button button;
    }

    [SerializeField] private ModeController modeController;
    [SerializeField] private ModeButtonEntry[] modeButtons;

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

        foreach (ModeButtonEntry entry in modeButtons)
        {
            AppMode mode = entry.mode;
            entry.button?.onClick.AddListener(() =>
            {
                modeController.SwitchMode(mode);
                ClosePanelAfterModeSelect();
            });
        }

        modeController.ModeChanged += UpdateButtonColors;
        UpdateButtonColors(modeController.CurrentMode);
    }

    private void UpdateButtonColors(AppMode currentMode)
    {
        foreach (ModeButtonEntry entry in modeButtons)
        {
            Color color = entry.mode == currentMode ? activeButtonColor : inactiveButtonColor;
            SetButtonColor(entry.button, color);
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private void ClosePanelAfterModeSelect()
    {
        if (autoClosePanelAfterModeSelect)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (modeController != null)
            modeController.ModeChanged -= UpdateButtonColors;
    }
}
