using UnityEngine;
public class TwinDatabase : MonoBehaviour
{
    public static TwinDatabase Instance { get; private set; }

    [SerializeField] private TwinGenerator twinGenerator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public Plant GetPlantById(string id)
    {
        TwinData data = twinGenerator?.TwinData;
        if (data?.rows == null) return null;

        foreach (Row row in data.rows)
        {
            if (row?.plants == null) continue;

            foreach (Plant p in row.plants)
            {
                if (p != null && p.plantId == id)
                    return p;
            }
        }

        return null;
    }
}