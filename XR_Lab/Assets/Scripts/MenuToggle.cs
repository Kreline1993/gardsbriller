using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    [Tooltip("Drag the newMenu GameObject here in the Inspector.")]
    public GameObject menuObject;

    void Update()
    {
        // Button.Start = menu button on controller / menu pinch gesture on hand tracking
        // Controller.LHand = left hand (controller or tracked hand)
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LHand))
        {
            menuObject.SetActive(!menuObject.activeSelf);
        }
    }
}
