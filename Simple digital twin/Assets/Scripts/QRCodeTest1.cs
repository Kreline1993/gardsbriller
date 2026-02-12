using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

public class QRCodeTest : MonoBehaviour
{
    [SerializeField]
    private GameObject qrPrefab;
    
    [SerializeField]
    private Vector3 spawnOffset = Vector3.zero;

    [Tooltip("When enabled, spawn height is relative to the real ground. Assign your XR Origin (with Tracking Origin Mode = Floor) so Y offset is 'height above ground'. Works with Guardian off but floor may drift over long distances outdoors.")]
    [SerializeField]
    private bool useDynamicGroundY = false;

    [Tooltip("Transform that represents floor level (e.g. your XR Origin). Only used when Use Dynamic Ground Y is enabled. With Tracking Origin Mode set to Floor, the XR Origin sits at ground level.")]
    [SerializeField]
    private Transform floorReference;

    [Tooltip("If your reference is at eye/head height (e.g. XR Origin in Device mode or with camera offset), set this to your height in metres so the script can find the ground below it. Example: 1.75 for 175 cm. Set to 0 if the reference is already at floor level.")]
    [SerializeField]
    private float heightFromReferenceToGround = 1.75f;

    private Dictionary<MRUKTrackable, GameObject> spawnedPrefabs = new();

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added: " + trackable.GetType().Name);

        if (spawnedPrefabs.ContainsKey(trackable))
            return;

        // Base spawn position with offset (XZ and rotation from trackable)
        Vector3 spawnPosition = trackable.transform.position + trackable.transform.TransformDirection(spawnOffset);

        // Optionally pin Y to ground level (floor reference) + vertical offset
        if (useDynamicGroundY && floorReference != null)
        {
            // Reference may be at eye height; subtract height to get actual ground
            float groundY = floorReference.position.y - heightFromReferenceToGround;
            spawnPosition.y = groundY + spawnOffset.y;
        }

        GameObject go = Instantiate(
            qrPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // go.transform.SetParent(trackable.transform, worldPositionStays: true); // Comment this out to keep the prefab at its initial position

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
