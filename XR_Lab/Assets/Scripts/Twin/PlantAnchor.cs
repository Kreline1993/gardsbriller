using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlantAnchor : MonoBehaviour
{
    [SerializeField] private PlantIdentity plantIdentity;
    [SerializeField] private PlantVisualHandle visualHandle;
    [SerializeField] private Transform baseVisualRoot;

    public string PlantId => plantIdentity != null ? plantIdentity.plantId : string.Empty;
    public string Species { get; private set; }
    public Vector3 LocalBottomCentre { get; private set; }
    public float LocalHeight { get; private set; }
    public Vector3 PlantLocalScale { get; private set; } = Vector3.one;
    public Transform BaseVisualRoot => baseVisualRoot;
    public GameObject ActiveInteractable { get; private set; }
    public bool HasActiveInteractable => ActiveInteractable != null;

    private void Awake()
    {
        if (plantIdentity == null)
            plantIdentity = GetComponent<PlantIdentity>();

        if (visualHandle == null)
            visualHandle = GetComponent<PlantVisualHandle>();
    }

    public void Initialize(
        string plantId,
        string species,
        Transform baseVisual,
        Vector3 localBottomCentre,
        float localHeight,
        Vector3 plantLocalScale)
    {
        if (plantIdentity == null)
            plantIdentity = GetComponent<PlantIdentity>();

        if (visualHandle == null)
            visualHandle = GetComponent<PlantVisualHandle>();

        if (plantIdentity != null)
            plantIdentity.plantId = plantId;

        Species = species;
        baseVisualRoot = baseVisual;
        LocalBottomCentre = localBottomCentre;
        LocalHeight = Mathf.Max(0f, localHeight);
        PlantLocalScale = plantLocalScale;

        visualHandle?.Configure(this, baseVisualRoot);
    }

    public void AttachInteractable(GameObject interactable)
    {
        ActiveInteractable = interactable;
        visualHandle?.AttachInteractable(interactable);
    }

    public GameObject DetachInteractable()
    {
        GameObject detached = ActiveInteractable;
        ActiveInteractable = null;
        visualHandle?.DetachInteractable();
        return detached;
    }

    public void CloseOpenPanels()
    {
        if (ActiveInteractable == null)
            return;

        InfoPanelSpawner[] panels = ActiveInteractable.GetComponentsInChildren<InfoPanelSpawner>(true);
        foreach (InfoPanelSpawner panel in panels)
        {
            if (panel != null)
                panel.ClosePanel();
        }
    }

    public (Vector3 bottomCentre, float height) GetWorldBounds()
    {
        Vector3 worldBottom = transform.TransformPoint(LocalBottomCentre);
        Vector3 worldTop = transform.TransformPoint(LocalBottomCentre + Vector3.up * LocalHeight);
        float worldHeight = Vector3.Distance(worldBottom, worldTop);
        return (worldBottom, worldHeight);
    }
}
