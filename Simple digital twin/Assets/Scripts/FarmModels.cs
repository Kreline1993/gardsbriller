using UnityEngine;

namespace FarmSystem.Models 
{
    [System.Serializable]
    public class LocationData {
        public float x;
        public float z;
    }

    [System.Serializable]
    public class PositionData {
        public float x;
        public float z; 
    }

    [System.Serializable]
    public class Plant {
        public string plantId;
        public string species;
        public PositionData position;
    }

    [System.Serializable]
    public class Row {
        public string rowId;
        public LocationData location;
        public Plant[] plants;
    }

    [System.Serializable]
    public class FarmData {
        public Row[] rows;
    }
}