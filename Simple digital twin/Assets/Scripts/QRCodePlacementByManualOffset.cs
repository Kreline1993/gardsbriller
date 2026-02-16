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
        string QRContent = GetQRCodeContent(trackable);
        if (QRContent == "QR000")
        {
            Debug.Log("QR code with ID QR000 detected. Spawning QR digital twin...");
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
        else
        {
            Debug.Log("QR code with ID " + QRContent + " detected. Not spawning QR digital twin.");
        }

    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (!spawnedPrefabs.TryGetValue(trackable, out var go))
            return;

        Destroy(go);
        spawnedPrefabs.Remove(trackable);
    }

    public string GetQRCodeContent(MRUKTrackable trackable)
    {
        // Get QR code content based on the trackable's properties
        Debug.Log("Getting QR code content for trackable: " + trackable.GetType().Name);
        var QRContent = trackable.MarkerPayloadString;
        Debug.Log("Trackable QR ID: " + QRContent);
        return QRContent; // Return just the payload string, not a formatted string
    }
}
