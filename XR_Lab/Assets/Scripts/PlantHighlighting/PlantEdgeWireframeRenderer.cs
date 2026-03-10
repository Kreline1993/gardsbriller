using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlantEdgeWireframeRenderer : MonoBehaviour
{
    [SerializeField] private Shader wireframeShader;
    [SerializeField, Min(0.5f)] private float edgeWidth = 1.5f;
    [SerializeField] private Color edgeColor = Color.white;
    [SerializeField] private bool visibleThroughObjects = false;

    private const string EdgeColorProperty = "_EdgeColor";
    private const string EdgeWidthProperty = "_EdgeWidth";
    private const string ZTestProperty = "_ZTest";

    private readonly List<GameObject> overlayObjects = new List<GameObject>();
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();
    private readonly List<Material> generatedMaterials = new List<Material>();

    private bool initialized;
    private bool initializeAttempted;

    public Shader WireframeShader
    {
        get => wireframeShader;
        set
        {
            if (wireframeShader == value)
                return;

            wireframeShader = value;
            Reinitialize();
        }
    }

    public float EdgeWidth
    {
        get => edgeWidth;
        set
        {
            edgeWidth = Mathf.Max(0.5f, value);
            ApplyMaterialProperties();
        }
    }

    public Color EdgeColor
    {
        get => edgeColor;
        set
        {
            edgeColor = value;
            ApplyMaterialProperties();
        }
    }

    public bool VisibleThroughObjects
    {
        get => visibleThroughObjects;
        set
        {
            visibleThroughObjects = value;
            ApplyMaterialProperties();
        }
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        SetOverlaysActive(true);
        ApplyMaterialProperties();
    }

    private void OnDisable()
    {
        SetOverlaysActive(false);
    }

    private void OnDestroy()
    {
        CleanupGeneratedResources();
    }

    private void OnValidate()
    {
        ApplyMaterialProperties();
    }

    public void SetEnabled(bool enabled)
    {
        if (!initialized)
            InitializeIfNeeded();

        if (initialized)
            SetOverlaysActive(enabled);
    }

    private void InitializeIfNeeded()
    {
        if (initialized || initializeAttempted)
            return;

        initializeAttempted = true;

        if (wireframeShader == null)
            wireframeShader = Shader.Find("Custom/Plant Edge Wireframe");

        if (wireframeShader == null)
        {
            Debug.LogWarning("[PlantEdgeWireframeRenderer] Shader 'Custom/Plant Edge Wireframe' not found.", this);
            return;
        }

        BuildOverlays();
        ApplyMaterialProperties();
        initialized = true;
    }

    private void Reinitialize()
    {
        CleanupGeneratedResources();
        initialized = false;
        initializeAttempted = false;
        InitializeIfNeeded();
    }

    private void BuildOverlays()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter sourceFilter = filters[i];
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
            if (sourceRenderer == null)
                continue;

            Mesh edgeMesh = BuildEdgeMesh(sourceFilter.sharedMesh);
            if (edgeMesh == null)
                continue;

            generatedMeshes.Add(edgeMesh);

            Material material = new Material(wireframeShader)
            {
                name = "PlantEdgeWireframe (Instance)"
            };
            generatedMaterials.Add(material);

            GameObject overlay = new GameObject("__PlantEdgeWireframe");
            overlay.transform.SetParent(sourceFilter.transform, false);
            overlay.layer = sourceFilter.gameObject.layer;

            MeshFilter overlayFilter = overlay.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = edgeMesh;

            MeshRenderer overlayRenderer = overlay.AddComponent<MeshRenderer>();
            overlayRenderer.sharedMaterial = material;
            overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            overlayRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            overlayObjects.Add(overlay);
        }
    }

    private Mesh BuildEdgeMesh(Mesh source)
    {
        int[] triangles = source.triangles;
        if (triangles == null || triangles.Length == 0)
            return null;

        Vector3[] srcVertices = source.vertices;
        Vector3[] srcNormals = source.normals;
        Vector4[] srcTangents = source.tangents;
        Vector2[] srcUv = source.uv;

        int triCount = triangles.Length / 3;

        // Phase 1: compute per-triangle face normals from vertex positions.
        Vector3[] faceNormals = new Vector3[triCount];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = srcVertices[triangles[i]];
            Vector3 b = srcVertices[triangles[i + 1]];
            Vector3 c = srcVertices[triangles[i + 2]];
            faceNormals[i / 3] = Vector3.Cross(b - a, c - a).normalized;
        }

        // Phase 2: build edge-to-triangle adjacency keyed by sorted vertex-index pair.
        // An edge shared by exactly 2 coplanar triangles is a triangulation diagonal to suppress.
        Dictionary<long, List<int>> edgeToTris = new Dictionary<long, List<int>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int t = i / 3;
            int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
            RegisterEdge(edgeToTris, i0, i1, t);
            RegisterEdge(edgeToTris, i1, i2, t);
            RegisterEdge(edgeToTris, i0, i2, t);
        }

        // Phase 3: mark coplanar shared edges for suppression.
        // Threshold: dot > 0.999 means the two triangles are on the same plane.
        const float CoplanarDot = 0.999f;
        HashSet<long> suppressed = new HashSet<long>();
        foreach (KeyValuePair<long, List<int>> kvp in edgeToTris)
        {
            if (kvp.Value.Count != 2)
                continue;
            float dot = Vector3.Dot(faceNormals[kvp.Value[0]], faceNormals[kvp.Value[1]]);
            if (dot > CoplanarDot)
                suppressed.Add(kvp.Key);
        }

        // Phase 4: build expanded mesh where every triangle has unique vertices
        //          and barycentric coordinates are encoded in UV2.
        //
        // Encoding rules (one component per edge):
        //   Component R (x): detects edge opposite vertex 0 (edge i1-i2)
        //   Component G (y): detects edge opposite vertex 1 (edge i0-i2)
        //   Component B (z): detects edge opposite vertex 2 (edge i0-i1)
        //
        // To SUPPRESS an edge: set the responsible component to 1 at all 3 vertices,
        // making it constant (=1) across the triangle so it never triggers detection.

        int vertexCount = triCount * 3;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = (srcNormals != null && srcNormals.Length == srcVertices.Length) ? new Vector3[vertexCount] : null;
        Vector4[] tangents = (srcTangents != null && srcTangents.Length == srcVertices.Length) ? new Vector4[vertexCount] : null;
        Vector2[] uv = (srcUv != null && srcUv.Length == srcVertices.Length) ? new Vector2[vertexCount] : null;
        Vector3[] bary = new Vector3[vertexCount];
        int[] newTriangles = new int[vertexCount];

        int outIndex = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];

            vertices[outIndex]     = srcVertices[i0];
            vertices[outIndex + 1] = srcVertices[i1];
            vertices[outIndex + 2] = srcVertices[i2];

            if (normals != null)
            {
                normals[outIndex]     = srcNormals[i0];
                normals[outIndex + 1] = srcNormals[i1];
                normals[outIndex + 2] = srcNormals[i2];
            }

            if (tangents != null)
            {
                tangents[outIndex]     = srcTangents[i0];
                tangents[outIndex + 1] = srcTangents[i1];
                tangents[outIndex + 2] = srcTangents[i2];
            }

            if (uv != null)
            {
                uv[outIndex]     = srcUv[i0];
                uv[outIndex + 1] = srcUv[i1];
                uv[outIndex + 2] = srcUv[i2];
            }

            // Start with standard barycentric assignment.
            Vector3 b0 = new Vector3(1f, 0f, 0f);
            Vector3 b1 = new Vector3(0f, 1f, 0f);
            Vector3 b2 = new Vector3(0f, 0f, 1f);

            // Suppress coplanar edges by clamping their component to 1 everywhere.
            if (suppressed.Contains(MakeEdgeKey(i1, i2))) { b0.x = 1f; b1.x = 1f; b2.x = 1f; }
            if (suppressed.Contains(MakeEdgeKey(i0, i2))) { b0.y = 1f; b1.y = 1f; b2.y = 1f; }
            if (suppressed.Contains(MakeEdgeKey(i0, i1))) { b0.z = 1f; b1.z = 1f; b2.z = 1f; }

            bary[outIndex]     = b0;
            bary[outIndex + 1] = b1;
            bary[outIndex + 2] = b2;

            newTriangles[outIndex]     = outIndex;
            newTriangles[outIndex + 1] = outIndex + 1;
            newTriangles[outIndex + 2] = outIndex + 2;
            outIndex += 3;
        }

        Mesh mesh = new Mesh { name = source.name + "_EdgeWire" };

        if (vertexCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        if (normals != null) mesh.normals = normals;
        if (tangents != null) mesh.tangents = tangents;
        if (uv != null) mesh.uv = uv;
        mesh.SetUVs(2, new List<Vector3>(bary));
        mesh.triangles = newTriangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static long MakeEdgeKey(int a, int b)
    {
        int lo = a < b ? a : b;
        int hi = a < b ? b : a;
        return ((long)lo << 32) | (uint)hi;
    }

    private static void RegisterEdge(Dictionary<long, List<int>> dict, int a, int b, int triIndex)
    {
        long key = MakeEdgeKey(a, b);
        if (!dict.TryGetValue(key, out List<int> list))
        {
            list = new List<int>(2);
            dict[key] = list;
        }
        list.Add(triIndex);
    }

    private void ApplyMaterialProperties()
    {
        for (int i = 0; i < generatedMaterials.Count; i++)
        {
            Material material = generatedMaterials[i];
            if (material == null)
                continue;

            material.SetColor(EdgeColorProperty, edgeColor);
            material.SetFloat(EdgeWidthProperty, edgeWidth);
            material.SetFloat(
                ZTestProperty,
                (float)(visibleThroughObjects
                    ? UnityEngine.Rendering.CompareFunction.Always
                    : UnityEngine.Rendering.CompareFunction.LessEqual));
        }
    }

    private void SetOverlaysActive(bool active)
    {
        for (int i = 0; i < overlayObjects.Count; i++)
        {
            if (overlayObjects[i] != null)
                overlayObjects[i].SetActive(active);
        }
    }

    private void CleanupGeneratedResources()
    {
        for (int i = 0; i < overlayObjects.Count; i++)
        {
            if (overlayObjects[i] != null)
                Destroy(overlayObjects[i]);
        }
        overlayObjects.Clear();

        for (int i = 0; i < generatedMaterials.Count; i++)
        {
            if (generatedMaterials[i] != null)
                Destroy(generatedMaterials[i]);
        }
        generatedMaterials.Clear();

        for (int i = 0; i < generatedMeshes.Count; i++)
        {
            if (generatedMeshes[i] != null)
                Destroy(generatedMeshes[i]);
        }
        generatedMeshes.Clear();
    }
}