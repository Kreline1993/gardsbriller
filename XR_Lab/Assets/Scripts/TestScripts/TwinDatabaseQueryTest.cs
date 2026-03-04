using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class TwinDatabaseQueryTest : MonoBehaviour
{
    public enum QueryMode
    {
        SpeciesEquals,
        HealthStatusEquals,
        GrowthAtLeast,
        LastWateredOnDate,
        GroundMoistureAtMost
    }

    [SerializeField] private QueryMode queryMode = QueryMode.SpeciesEquals;
    [SerializeField] private string species = "Tomato";
    [SerializeField] private string healthStatus = "bad";
    [SerializeField] private int minimumGrowth = 90;
    [SerializeField] private int wateredDay = 3;
    [SerializeField] private int wateredMonth = 3;
    [SerializeField] private int wateredYear = 2026;
    [SerializeField] private int maxGroundMoisture = 30;
    [SerializeField] private float waitForDataTimeoutSeconds = 5f;

    private IEnumerator Start()
    {
        yield return StartCoroutine(WaitForDatabaseData());

        if (TwinDatabase.Instance == null)
        {
            Debug.LogError("[TwinDatabaseQueryTest] TwinDatabase.Instance is null.");
            yield break;
        }

        List<Plant> selectedPlants = RunQuery();
        LogPlants(selectedPlants);
    }

    private IEnumerator WaitForDatabaseData()
    {
        float elapsed = 0f;

        while (elapsed < waitForDataTimeoutSeconds)
        {
            if (TwinDatabase.Instance != null)
            {
                List<Row> rows = TwinDatabase.Instance.GetRowsWhere(r => true);
                if (rows.Count > 0)
                    yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[TwinDatabaseQueryTest] Timed out waiting for TwinData. Query may return empty results.");
    }

    private List<Plant> RunQuery()
    {
        switch (queryMode)
        {
            case QueryMode.SpeciesEquals:
                return TwinDatabase.Instance.GetPlantsWhere(p => p.species == species);

            case QueryMode.HealthStatusEquals:
                return TwinDatabase.Instance.GetPlantsWhere(p => p.healthStatus == healthStatus);

            case QueryMode.GrowthAtLeast:
                return TwinDatabase.Instance.GetPlantsWhere(p => p.growth >= minimumGrowth);

            case QueryMode.LastWateredOnDate:
                return TwinDatabase.Instance.GetPlantsWhere(p =>
                    TwinDatabase.IsSameDate(p.lastWateredDate, wateredDay, wateredMonth, wateredYear));

            case QueryMode.GroundMoistureAtMost:
                return TwinDatabase.Instance.GetPlantsWhere((p, row) => row.groundMoisture <= maxGroundMoisture);

            default:
                return new List<Plant>();
        }
    }

    private void LogPlants(List<Plant> plants)
    {
        if (plants == null || plants.Count == 0)
        {
            Debug.Log($"[TwinDatabaseQueryTest] No plants matched query mode: {queryMode}.");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[TwinDatabaseQueryTest] Query mode: {queryMode} | Matches: {plants.Count}");

        for (int i = 0; i < plants.Count; i++)
        {
            Plant plant = plants[i];
            if (plant == null) continue;

            string watered = plant.lastWateredDate == null
                ? "n/a"
                : $"{plant.lastWateredDate.day:D2}-{plant.lastWateredDate.month:D2}-{plant.lastWateredDate.year}";

            builder.AppendLine($"- {plant.plantId} | {plant.plantName} | {plant.species} | growth={plant.growth} | health={plant.healthStatus} | watered={watered}");
        }

        Debug.Log(builder.ToString());
    }
}
