using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

public class QRCodePlacementByManualOffset : MonoBehaviour
{
    [SerializeField]
    private GameObject qrPrefab;
    
    [SerializeField]
    private Vector3 spawnOffset = Vector3.zero;

    private Dictionary<MRUKTrackable, GameObject> spawnedPrefabs = new();

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added: " + trackable.GetType().Name);

        if (spawnedPrefabs.ContainsKey(trackable))
            return;

        // Calculate spawn position with offset
        Vector3 spawnPosition = trackable.transform.position + trackable.transform.TransformDirection(spawnOffset);
        
        GameObject go = Instantiate(
            qrPrefab,
            spawnPosition,
            trackable.transform.rotation
        );

        //go.transform.SetParent(trackable.transform, worldPositionStays: true); // Comment this out to keep the prefab at its initial position

        spawnedPrefabs.Add(trackable, go);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (!spawnedPrefabs.TryGetValue(trackable, out var go))
            return;

        Destroy(go);
        spawnedPrefabs.Remove(trackable);
    }
}
