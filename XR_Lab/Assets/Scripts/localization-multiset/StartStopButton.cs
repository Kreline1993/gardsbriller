using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartStopButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MultisetUpdater multisetUpdater;
    [SerializeField] private ToastUI toastUI;

    [Header("Button Visuals")]
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Sprite startSprite;
    [SerializeField] private Sprite stopSprite;

    private bool isRunning = false;

    public void Toggle()
    {
        isRunning = !isRunning;

        if (isRunning)
        {
            multisetUpdater.StartAutomaticUpdating();
            buttonLabel.text = "Stop";
            buttonIcon.sprite = stopSprite;
            if (toastUI != null)
                toastUI.ShowToast("Starting Automatic Updating");
        }
        else
        {
            multisetUpdater.PauseAutomaticUpdating();
            buttonLabel.text = "Start";
            buttonIcon.sprite = startSprite;
            if (toastUI != null)
                toastUI.ShowToast("Stopping Automatic Updating");
        }
    }
}
