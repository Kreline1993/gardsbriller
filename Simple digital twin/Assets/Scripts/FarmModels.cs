using UnityEngine;

[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}

// New class to handle width/length and Diameter
[System.Serializable]
public class SizeData
{
    public float width;   // Used by Row
    public float length;  // Used by Row
    public float Diameter; // Used by Plant (Note: matches JSON casing)
}

[System.Serializable]
public class Plant
{
    public string plantId;
    public string species;
    public Vector3Data position;
    public SizeData size; // Added to access plant diameter
}

[System.Serializable]
public class Row
{
    public string rowId;
    public Vector3Data location;
    public SizeData size; // Added to access row dimensions
    public Plant[] plants;
}

[System.Serializable]
public class FarmData
{
    public Row[] rows;
}
