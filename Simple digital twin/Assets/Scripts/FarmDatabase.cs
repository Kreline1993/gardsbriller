using UnityEngine;
public class FarmDatabase : MonoBehaviour
{
    public static FarmDatabase Instance { get; private set; }

    [SerializeField] private FarmFieldGenerator farmFieldGenerator;

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
        FarmData data = farmFieldGenerator?.FarmData;
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