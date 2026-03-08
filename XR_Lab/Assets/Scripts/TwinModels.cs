using UnityEngine;

[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}


[System.Serializable]
public class SizeData
{
    public float width;
    public float length;
    public float diameter;
    public float height;
}

[System.Serializable]
public class DateData
{
    public int day;
    public int month;
    public int year;
}

[System.Serializable]
public class NoteData
{
    public string textNote;
    public string noteTag;
}

[System.Serializable]
public class Plant
{
    public string plantId;
    public string plantName;
    public string species;
    public Vector3Data position;
    public SizeData size;
    public int growth;
    public DateData plantedDate;
    public DateData estimatedHarvestDate;
    public DateData lastWateredDate;
    public string healthStatus;
    public NoteData notes;
    public DateData lastPesticide;
    public DateData nextPesticide;
}

[System.Serializable]
public class Row
{
    public string rowId;
    public Vector3Data location;
    public int groundMoisture;
    public SizeData size;
    public Plant[] plants;
}

[System.Serializable]
public class TwinData
{
    public Row[] rows;
}