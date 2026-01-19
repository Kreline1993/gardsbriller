using UnityEngine;

namespace FarmSystem.Models 
{
[System.Serializable]
    public class Vector3Data {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Plant {
        public string plantId;
        public string species;
        public Vector3Data position;
    }

    [System.Serializable]
    public class Row {
        public string rowId;
        public Vector3Data location;
        public Plant[] plants;
    }

    [System.Serializable]
    public class FarmData {
        public Row[] rows;
    }
}