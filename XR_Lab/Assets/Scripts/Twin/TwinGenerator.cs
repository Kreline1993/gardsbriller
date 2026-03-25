using UnityEngine;
using System.Collections;

public class TwinGenerator : MonoBehaviour
{
    public GameObject interactionPrefab;
    public float scaleFactor = 10.0f;
    [SerializeField] private TwinDataLoader twinDataLoader;

    public TwinData TwinData { get; private set; }

    private void Awake()
    {
        if (twinDataLoader == null)
            twinDataLoader = GetComponent<TwinDataLoader>();
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

    void GenerateFromData(TwinData data)
    {
        TwinData = data;
        if (TwinData?.rows == null) return;

        foreach (Row row in TwinData.rows)
        {
            foreach (Plant p in row.plants)
            {
                Vector3 localOffset = new Vector3(
                    p.position.x * scaleFactor,
                    p.position.y * scaleFactor,
                    p.position.z * scaleFactor
                );
                Vector3 worldPos = transform.TransformPoint(localOffset);

                GameObject ghostPlant = Instantiate(interactionPrefab, worldPos, transform.rotation, this.transform);

                if (p.size != null)
                {
                    float d = p.size.diameter * scaleFactor;
                    float h = p.size.height * scaleFactor;
                    Mesh mesh = interactionPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                    float meshHeight = (mesh != null) ? mesh.bounds.size.y : 1f;
                    ghostPlant.transform.localScale = new Vector3(d, h / meshHeight, d);

                    // Shift up by half the final height so the mesh bottom sits at ground level
                    Vector3 lp = ghostPlant.transform.localPosition;
                    lp.y += h * 0.5f;
                    ghostPlant.transform.localPosition = lp;
                }

                PlantIdentity identity = ghostPlant.GetComponent<PlantIdentity>();
                if (identity != null)
                {
                    identity.plantId = p.plantId;
                }
                ghostPlant.name = $"Trigger_{p.species}_{p.plantId}";
            }
        }
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
                        Vector3 pPos = new Vector3(p.position.x, p.position.y, p.position.z) * scaleFactor;
                        float radius = (p.size != null ? p.size.diameter : 0.1f) * scaleFactor * 0.5f;
                        Gizmos.DrawWireSphere(pPos, radius);
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}