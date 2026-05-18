using UnityEngine;
using System.Collections;

public class TwinGenerator : MonoBehaviour
{
    public GameObject interactionPrefab;
    [SerializeField] private GameObject baseVisualPrefab;
    public float scaleFactor = 10.0f;
    [SerializeField] private TwinDataLoader twinDataLoader;
    [SerializeField] private PlantVisualRegistry plantVisualRegistry;
    [SerializeField] private PlantInteractionSpawnController interactionSpawnController;
    [SerializeField, Min(0f)] private float interactionSpawnDistance = 10f;
    [SerializeField, Min(0f)] private float interactionDespawnDistance = 12f;
    [SerializeField, Min(0.05f)] private float interactionUpdateInterval = 0.5f;

    public TwinData TwinData { get; private set; }

    private void Awake()
    {
        if (twinDataLoader == null)
            twinDataLoader = GetComponent<TwinDataLoader>();

        if (plantVisualRegistry == null)
            plantVisualRegistry = GetComponent<PlantVisualRegistry>();

        if (plantVisualRegistry == null)
            plantVisualRegistry = gameObject.AddComponent<PlantVisualRegistry>();

        if (interactionSpawnController == null)
            interactionSpawnController = GetComponent<PlantInteractionSpawnController>();

        if (interactionSpawnController == null)
            interactionSpawnController = gameObject.AddComponent<PlantInteractionSpawnController>();
    }

    void Start()
    {
        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        if (twinDataLoader == null)
        {
            Debug.LogError("[TwinGenerator] Missing TwinDataLoader reference.");
            yield break;
        }

        TwinData loadedData = null;
        yield return StartCoroutine(twinDataLoader.LoadTwinDataRoutine(data => loadedData = data));

        if (loadedData == null)
            yield break;

        GenerateFromData(loadedData);
    }

    Vector3 ComputePlantLocalPosition(Plant p)
    {
        Vector3 pos = new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
        if (p.size != null)
            pos.y += p.size.height * scaleFactor * 0.5f;
        return pos;
    }

    void GenerateFromData(TwinData data)
    {
        TwinData = data;
        if (TwinData?.rows == null) return;

        if (plantVisualRegistry == null)
            plantVisualRegistry = GetComponent<PlantVisualRegistry>();

        if (interactionSpawnController == null)
            interactionSpawnController = GetComponent<PlantInteractionSpawnController>();

        if (interactionSpawnController == null)
            interactionSpawnController = gameObject.AddComponent<PlantInteractionSpawnController>();

        Mesh prefabMesh = interactionPrefab != null ? interactionPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;
        float prefabMeshHeight = (prefabMesh != null) ? prefabMesh.bounds.size.y : 1f;
        var anchors = new System.Collections.Generic.List<PlantAnchor>();

        foreach (Row row in TwinData.rows)
        {
            foreach (Plant p in row.plants)
            {
                Vector3 localOffset = ComputePlantLocalPosition(p);
                Vector3 plantScale = ComputePlantScale(p, prefabMeshHeight);

                PlantAnchor anchor = CreatePlantAnchor(p, localOffset, plantScale, prefabMeshHeight);
                if (anchor != null)
                    anchors.Add(anchor);
            }
        }

        if (plantVisualRegistry != null)
            plantVisualRegistry.RegisterAnchors(anchors);

        if (interactionSpawnController != null)
        {
            interactionSpawnController.Configure(
                interactionPrefab,
                anchors,
                interactionSpawnDistance,
                interactionDespawnDistance,
                interactionUpdateInterval);
        }
    }

    Vector3 ComputePlantScale(Plant p, float prefabMeshHeight)
    {
        if (p.size == null)
            return Vector3.one;

        float diameter = p.size.diameter * scaleFactor;
        float height = p.size.height * scaleFactor;
        float yScale = prefabMeshHeight > 0f ? height / prefabMeshHeight : height;
        return new Vector3(diameter, yScale, diameter);
    }

    PlantAnchor CreatePlantAnchor(Plant p, Vector3 localOffset, Vector3 plantScale, float prefabMeshHeight)
    {
        GameObject anchorObject = new GameObject($"PlantAnchor_{p.species}_{p.plantId}");
        anchorObject.transform.SetParent(transform, false);
        anchorObject.transform.localPosition = localOffset;
        anchorObject.transform.localRotation = Quaternion.identity;
        anchorObject.transform.localScale = Vector3.one;

        PlantIdentity identity = anchorObject.AddComponent<PlantIdentity>();
        identity.plantId = p.plantId;

        PlantVisualHandle handle = anchorObject.AddComponent<PlantVisualHandle>();
        PlantAnchor anchor = anchorObject.AddComponent<PlantAnchor>();

        Transform baseVisual = CreateBaseVisual(anchorObject.transform, plantScale);
        CalculateLocalBounds(baseVisual, anchorObject.transform, prefabMeshHeight * plantScale.y, out Vector3 localBottomCentre, out float localHeight);
        anchor.Initialize(p.plantId, p.species, baseVisual, localBottomCentre, localHeight, plantScale);
        handle.InitializeIfNeeded();
        return anchor;
    }

    Transform CreateBaseVisual(Transform parent, Vector3 plantScale)
    {
        Transform visualRoot;
        if (baseVisualPrefab != null)
        {
            GameObject instance = Instantiate(baseVisualPrefab, parent);
            visualRoot = instance.transform;
        }
        else
        {
            visualRoot = BuildVisualFromInteractionPrefab(parent);
        }

        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = plantScale;
        return visualRoot;
    }

    Transform BuildVisualFromInteractionPrefab(Transform parent)
    {
        GameObject visual = new GameObject("BaseVisual");
        visual.transform.SetParent(parent, false);

        MeshFilter sourceMeshFilter = interactionPrefab != null ? interactionPrefab.GetComponentInChildren<MeshFilter>() : null;
        MeshRenderer sourceRenderer = interactionPrefab != null ? interactionPrefab.GetComponentInChildren<MeshRenderer>() : null;

        MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceMeshFilter != null ? sourceMeshFilter.sharedMesh : null;

        MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
        if (sourceRenderer != null)
        {
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = sourceRenderer.receiveShadows;
            meshRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            meshRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            meshRenderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;
        }

        return visual.transform;
    }

    static void CalculateLocalBounds(Transform visualRoot, Transform anchorTransform, float fallbackHeight, out Vector3 localBottomCentre, out float localHeight)
    {
        Renderer[] renderers = visualRoot != null ? visualRoot.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers == null || renderers.Length == 0)
        {
            localHeight = Mathf.Max(0f, fallbackHeight);
            localBottomCentre = new Vector3(0f, -localHeight * 0.5f, 0f);
            return;
        }

        Vector3 localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        bool foundRenderer = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Bounds bounds = renderer.bounds;
            Vector3[] corners =
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = anchorTransform.InverseTransformPoint(corner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
                foundRenderer = true;
            }
        }

        if (!foundRenderer)
        {
            localHeight = Mathf.Max(0f, fallbackHeight);
            localBottomCentre = new Vector3(0f, -localHeight * 0.5f, 0f);
            return;
        }

        localHeight = Mathf.Max(0f, localMax.y - localMin.y);
        localBottomCentre = new Vector3((localMin.x + localMax.x) * 0.5f, localMin.y, (localMin.z + localMax.z) * 0.5f);
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        if (twinDataLoader == null)
            twinDataLoader = GetComponent<TwinDataLoader>();

        if (twinDataLoader == null) return;

        if (twinDataLoader.TryLoadTwinDataEditor(out TwinData data) && data.rows != null)
        {

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            foreach (Row row in data.rows)
            {
                Vector3 rowBase = new Vector3(row.location.x, row.location.y, row.location.z) * scaleFactor;

                float w = (row.size != null ? row.size.width : 1f) * scaleFactor;
                float l = (row.size != null ? row.size.length : 1f) * scaleFactor;
                float h = 0.2f;

                Vector3 rowCenter = rowBase + new Vector3(w / 2f, h / 2f, l / 2f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(rowCenter, new Vector3(w, h, l));

                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawCube(rowCenter, new Vector3(w, h, l));

                if (row.plants != null)
                {
                    Gizmos.color = Color.green;
                    foreach (Plant p in row.plants)
                    {
                        Vector3 pPos = ComputePlantLocalPosition(p);
                        float radius = (p.size != null ? p.size.diameter : 0.1f) * scaleFactor * 0.5f;
                        Gizmos.DrawWireSphere(pPos, radius);
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
