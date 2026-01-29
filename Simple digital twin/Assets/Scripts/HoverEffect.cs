using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

[RequireComponent(typeof(XRSimpleInteractable))] // Adds this component to the interactable if not present.
public class HoverHighlight : MonoBehaviour
{
    private XRSimpleInteractable _interactable;
    private Outline _outline;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _outline = GetComponent<Outline>();

        if (_outline != null) _outline.enabled = false; // Disables the outline if it exists when the object is enabled.
    }

    void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEnter); // Adds a listener to the hover entered event when the object is enabled.
        _interactable.hoverExited.AddListener(OnHoverExit); // Adds a listener to the hover exited event when the object is enabled.
    }

    void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter); // Removes a listener to the hover entered event to avoid memory leaks.
        _interactable.hoverExited.RemoveListener(OnHoverExit); // Removes a listener to the hover exited event to avoid memory leaks.
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (_outline != null) _outline.enabled = true; // Enables the outline if it exists when hovering over the object.
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (_outline != null) _outline.enabled = false; // Disables the outline if it exists when hovering away from the object.
    }
}