/// <summary>
/// Implement this on any component that provides a visual highlight for a plant GameObject.
///
/// How to wire it up:
///   1. Create a component (e.g. PlantOutlineHighlighter) that implements this interface.
///   2. In that component's Start(), call:
///        PlantHighlightController.Instance.RegisterHighlighter(this);
///   3. Implement SetHighlight to turn your visual on/off based on the plantId.
///
/// Example using the QuickOutline asset already in your project:
///   public void SetHighlight(string plantId, bool highlighted)
///   {
///       if (GetComponent<PlantIdentity>()?.plantId != plantId) return;
///       var outline = GetComponent<Outline>();
///       if (outline != null) outline.enabled = highlighted;
///   }
/// </summary>
public interface IPlantHighlighter
{
    /// <summary>Turn the highlight on or off for the plant with the given ID.</summary>
    void SetHighlight(string plantId, bool highlighted);

    /// <summary>Turn off all highlights this component is responsible for.</summary>
    void ClearAllHighlights();
}