using UnityEngine;
using System;
using System.Collections.Generic;

public class TwinDatabase : MonoBehaviour
{
    public static TwinDatabase Instance { get; private set; }

    [SerializeField] private TwinGenerator twinGenerator;

    private readonly Dictionary<string, Plant> plantsById = new Dictionary<string, Plant>();
    private TwinData indexedData;

    public IReadOnlyDictionary<string, Plant> PlantsById => plantsById;
    public Plant this[string plantId] => GetPlantById(plantId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void EnsureIndex()
    {
        TwinData currentData = twinGenerator?.TwinData;
        if (currentData == null || currentData.rows == null)
        {
            plantsById.Clear();
            indexedData = null;
            return;
        }

        if (ReferenceEquals(indexedData, currentData) && plantsById.Count > 0)
            return;

        plantsById.Clear();

        foreach (Row row in currentData.rows)
        {
            if (row?.plants == null) continue;

            foreach (Plant plant in row.plants)
            {
                if (plant == null || string.IsNullOrEmpty(plant.plantId)) continue;
                plantsById[plant.plantId] = plant;
            }
        }

        indexedData = currentData;
    }

    public Plant GetPlantById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        EnsureIndex();
        plantsById.TryGetValue(id, out Plant plant);
        return plant;
    }

    public bool TryGetPlantById(string id, out Plant plant)
    {
        plant = null;
        if (string.IsNullOrEmpty(id)) return false;

        EnsureIndex();
        return plantsById.TryGetValue(id, out plant);
    }

    public List<Plant> GetPlantsBySpecies(string species)
    {
        List<Plant> results = new List<Plant>();
        if (string.IsNullOrEmpty(species)) return results;

        EnsureIndex();

        foreach (Plant plant in plantsById.Values)
        {
            if (plant != null && plant.species == species)
                results.Add(plant);
        }

        return results;
    }

    public List<Plant> GetPlantsWhere(Func<Plant, bool> predicate)
    {
        List<Plant> results = new List<Plant>();
        if (predicate == null) return results;

        EnsureIndex();

        foreach (Plant plant in plantsById.Values)
        {
            if (plant != null && predicate(plant))
                results.Add(plant);
        }

        return results;
    }

    public List<Row> GetRowsWhere(Func<Row, bool> predicate)
    {
        List<Row> results = new List<Row>();
        if (predicate == null) return results;

        TwinData data = twinGenerator?.TwinData;
        if (data?.rows == null) return results;

        foreach (Row row in data.rows)
        {
            if (row != null && predicate(row))
                results.Add(row);
        }

        return results;
    }

    public List<Plant> GetPlantsWhere(Func<Plant, Row, bool> predicate)
    {
        List<Plant> results = new List<Plant>();
        if (predicate == null) return results;

        TwinData data = twinGenerator?.TwinData;
        if (data?.rows == null) return results;

        foreach (Row row in data.rows)
        {
            if (row?.plants == null) continue;

            foreach (Plant plant in row.plants)
            {
                if (plant != null && predicate(plant, row))
                    results.Add(plant);
            }
        }

        return results;
    }

    public static bool IsSameDate(DateData date, int day, int month, int year)
    {
        return date != null && date.day == day && date.month == month && date.year == year;
    }

    public static bool IsDateBeforeOrEqual(DateData date, int day, int month, int year)
    {
        if (date == null) return false;

        int lhs = date.year * 10000 + date.month * 100 + date.day;
        int rhs = year * 10000 + month * 100 + day;
        return lhs <= rhs;
    }
}