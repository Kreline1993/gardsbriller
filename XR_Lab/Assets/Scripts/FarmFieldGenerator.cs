using UnityEngine;
using System.Collections;

public class FarmFieldGenerator : MonoBehaviour
{
    public GameObject interactionPrefab;
    public float scaleFactor = 10.0f;
    [SerializeField] private FarmDataLoader farmDataLoader;

    public FarmData FarmData { get; private set; }

    private void Awake()
    {
        if (farmDataLoader == null)
            farmDataLoader = GetComponent<FarmDataLoader>();
    }

    void Start()
    {
        StartCoroutine(GenerateFieldRoutine());
    }

    IEnumerator GenerateFieldRoutine()
    {
        if (farmDataLoader == null)
        {
            Debug.LogError("[FarmFieldGenerator] Missing FarmDataLoader reference.");
            yield break;
        }

        FarmData loadedData = null;
        yield return StartCoroutine(farmDataLoader.LoadFarmDataRoutine(data => loadedData = data));

        if (loadedData == null)
            yield break;

        GenerateFromData(loadedData);
    }

    void GenerateFromData(FarmData data)
    {
        FarmData = data;
        if (FarmData?.rows == null) return;

        foreach (Row row in FarmData.rows)
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
        if (farmDataLoader == null)
            farmDataLoader = GetComponent<FarmDataLoader>();

        if (farmDataLoader == null) return;

        if (farmDataLoader.TryLoadFarmDataEditor(out FarmData data) && data.rows != null)
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
                        Gizmos.DrawWireSphere(pPos, 0.1f * scaleFactor);
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}