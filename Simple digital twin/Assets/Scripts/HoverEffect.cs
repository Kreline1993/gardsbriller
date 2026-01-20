using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

[RequireComponent(typeof(XRSimpleInteractable))]
public class HoverHighlight : MonoBehaviour
{
    private XRSimpleInteractable _interactable;
    private Outline _outline;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _outline = GetComponent<Outline>();

        // Outline is off at the start
        if (_outline != null) _outline.enabled = false;
    }

    void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);
    }

    void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHoverEnter);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (_outline != null) _outline.enabled = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (_outline != null) _outline.enabled = false;
    }
}