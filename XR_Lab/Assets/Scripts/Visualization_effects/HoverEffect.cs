using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(InteractableUnityEventWrapper))] // Adds this component to the interactable if not present.
public class HoverHighlight : MonoBehaviour
{
    private InteractableUnityEventWrapper _interactableEvents;
    private Outline _outline;

    void Awake()
    {
        _interactableEvents = GetComponent<InteractableUnityEventWrapper>();
        _outline = GetComponent<Outline>();

        if (_outline != null) _outline.enabled = false; // Disables the outline if it exists when the object is enabled.
    }

    void OnEnable()
    {
        _interactableEvents.WhenHover.AddListener(OnHoverEnter); // Adds a listener to the hover entered event when the object is enabled.
        _interactableEvents.WhenUnhover.AddListener(OnHoverExit); // Adds a listener to the hover exited event when the object is enabled.
    }

    void OnDisable()
    {
        _interactableEvents.WhenHover.RemoveListener(OnHoverEnter); // Removes a listener to the hover entered event to avoid memory leaks.
        _interactableEvents.WhenUnhover.RemoveListener(OnHoverExit); // Removes a listener to the hover exited event to avoid memory leaks.
    }

    private void OnHoverEnter()
    {
        if (_outline != null) _outline.enabled = true; // Enables the outline if it exists when hovering over the object.
    }

    private void OnHoverExit()
    {
        if (_outline != null) _outline.enabled = false; // Disables the outline if it exists when hovering away from the object.
    }
}