using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    [Tooltip("Drag the newMenu GameObject here in the Inspector.")]
    public GameObject menuObject;

    [Tooltip("Any additional panels that should close with the menu. They will NOT reopen when the menu reopens.")]
    [SerializeField] private GameObject[] additionalPanels;

    void Update()
    {
        // Button.Start = menu button on controller / menu pinch gesture on hand tracking
        // Controller.LHand = left hand (controller or tracked hand)
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LHand))
        {
            bool opening = !menuObject.activeSelf;

            menuObject.SetActive(opening);

            // Always close all additional panels — they never auto-reopen.
            if (!opening && additionalPanels != null)
            {
                foreach (GameObject panel in additionalPanels)
                {
                    if (panel != null)
                        panel.SetActive(false);
                }
            }
        }
    }
}
