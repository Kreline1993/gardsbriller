using System;
using System.Collections.Generic;

[Serializable]
public class OverviewSummarySectionData
{
    public int totalRows;
    public int totalPlants;
    public int lowMoistureRows;
    public int badHealthPlants;
    public int warningPlants;
    public int ripePlants;
}

[Serializable]
public class OverviewRowSectionData
{
    public string rowId;
    public int groundMoisture;
    public int plantCount;
}

[Serializable]
public class OverviewPlantSectionData
{
    public string plantId;
    public string species;
    public string rowId;
    public int growth;
    public string healthStatus;
    public string noteTag;
}

[Serializable]
public class OverviewPanelDataSnapshot
{
    public OverviewSummarySectionData summary = new OverviewSummarySectionData();
    public List<OverviewRowSectionData> lowMoistureRows = new List<OverviewRowSectionData>();
    public List<OverviewPlantSectionData> badHealthPlants = new List<OverviewPlantSectionData>();
    public List<OverviewPlantSectionData> warningPlants = new List<OverviewPlantSectionData>();
    public List<OverviewPlantSectionData> ripePlants = new List<OverviewPlantSectionData>();
    public string nextPesticidesDate = "N/A";
    public string lastPesticideDate = "N/A";
    public string lastWateredDate = "N/A";
    public int lowestRowMoisture = 0;
}
