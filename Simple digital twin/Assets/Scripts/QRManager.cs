using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;

// --- Data models for JSON deserialization ---

[System.Serializable]
public class QRMarker
{
    public string id;
    public Vector3Data position;
}

[System.Serializable]
public class QRMarkerDataFile
{
    public QRMarker[] markers;
}

// --- Main manager ---

public class QRManager : MonoBehaviour
{
    [SerializeField]
    private string markerDataFileName = "QRMarkerData.json";

    // Known digital twin positions keyed by marker ID
    private Dictionary<string, Vector3> markerLookup = new();

    // Real-world tracked positions keyed by marker ID
    private Dictionary<string, Vector3> trackedMarkers = new();

    // Reverse lookup: trackable -> marker ID (for removal)
    private Dictionary<MRUKTrackable, string> trackableToId = new();

    // Computed transformation
    private Quaternion computedRotation;
    private Vector3 computedTranslation;
    private bool transformComputed = false;

    [Header("UI Display")]
[SerializeField] private TextMeshProUGUI debugTextMesh;
[SerializeField] private GameObject debugPanel; // The Canvas object

public void UpdateVisualDisplay()
{
    if (!transformComputed)
    {
        debugTextMesh.text = "Transformation not yet computed.\nScan 2 markers.";
        return;
    }

    // Get the user's current head position (tracking space)
    Vector3 headPos = Camera.main.transform.position;
    
    // Convert to Digital Twin space using your existing method
    if (TryTransformToDigitalTwin(headPos, out Vector3 dtPos))
    {
        debugTextMesh.text = $"<b>Digital Twin Position:</b>\n" +
                             $"X: {dtPos.x:F3}\n" +
                             $"Y: {dtPos.y:F3}\n" +
                             $"Z: {dtPos.z:F3}\n" +
                             $"<color=green>System Calibrated</color>";
    }
}

public void ToggleDebugUI()
{
    bool isActive = !debugPanel.activeSelf;
    debugPanel.SetActive(isActive);

    if (isActive)
    {
        // Move the panel 1 meter in front of the camera
        Transform cam = Camera.main.transform;
        debugPanel.transform.position = cam.position + (cam.forward * 1.0f);
        debugPanel.transform.rotation = Quaternion.LookRotation(debugPanel.transform.position - cam.position);
        Debug.Log("[QRManager] Displaying debug panel");
        
        UpdateVisualDisplay();
    }
    else
    {
        Debug.Log("[QRManager] Hiding debug panel");
    }
}

    void Start()
    {
        StartCoroutine(LoadMarkerData());
    }

    IEnumerator LoadMarkerData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, markerDataFileName);
        string jsonString = "";

        // Android/Quest: path contains ://
        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(path))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[QRManager] Error loading marker data from APK: {webRequest.error}");
                    yield break;
                }
                jsonString = webRequest.downloadHandler.text;
            }
        }
        else
        {
            // Editor / standalone PC
            if (!File.Exists(path))
            {
                Debug.LogError($"[QRManager] Marker data not found at: {path}");
                yield break;
            }
            jsonString = File.ReadAllText(path);
        }

        ParseMarkerData(jsonString);
    }

    void ParseMarkerData(string json)
    {
        QRMarkerDataFile dataFile = JsonUtility.FromJson<QRMarkerDataFile>(json);
        if (dataFile == null || dataFile.markers == null)
        {
            Debug.LogError("[QRManager] Failed to parse marker data JSON.");
            return;
        }

        markerLookup.Clear();
        foreach (QRMarker marker in dataFile.markers)
        {
            Vector3 pos = new Vector3(marker.position.x, marker.position.y, marker.position.z);
            markerLookup[marker.id] = pos;
            Debug.Log($"[QRManager] Loaded marker '{marker.id}' at digital twin position {pos}");
        }

        Debug.Log($"[QRManager] Loaded {markerLookup.Count} markers from {markerDataFileName}");
    }

    // --- MRUK callbacks (wire these in the Inspector) ---

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        // Guard: only QR codes
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        // Guard: must have payload
        string payload = trackable.MarkerPayloadString;
        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogWarning("[QRManager] QR code detected but payload is empty, skipping.");
            return;
        }

        // Guard: must be a known marker
        if (!markerLookup.ContainsKey(payload))
        {
            Debug.LogWarning($"[QRManager] QR code '{payload}' not found in marker data, skipping.");
            return;
        }

        // Store real-world tracked position
        Vector3 realWorldPos = trackable.transform.position;
        trackedMarkers[payload] = realWorldPos;
        trackableToId[trackable] = payload;

        Debug.Log($"[QRManager] Tracked marker '{payload}' at real-world position {realWorldPos}");

        TryComputeTransformation();
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (!trackableToId.TryGetValue(trackable, out string id))
            return;

        trackedMarkers.Remove(id);
        trackableToId.Remove(trackable);
        transformComputed = false;

        Debug.Log($"[QRManager] Marker '{id}' removed. Transformation invalidated.");
    }

    // --- Transformation computation ---

    void TryComputeTransformation()
    {
        if (trackedMarkers.Count < 2)
        {
            Debug.Log($"[QRManager] {trackedMarkers.Count}/2 markers tracked. Waiting for more...");
            return;
        }

        // Pick the first two tracked markers
        var ids = trackedMarkers.Keys.ToArray();
        string idA = ids[0];
        string idB = ids[1];

        Vector3 realA = trackedMarkers[idA];
        Vector3 realB = trackedMarkers[idB];
        Vector3 dtA = markerLookup[idA];
        Vector3 dtB = markerLookup[idB];

        // Step 1: Rotation - minimum rotation mapping real direction to digital twin direction
        Vector3 realDir = realB - realA;
        Vector3 dtDir = dtB - dtA;
        computedRotation = Quaternion.FromToRotation(realDir, dtDir);

        // Step 2: Translation
        computedTranslation = dtA - computedRotation * realA;

        transformComputed = true;

        // XR Origin is at Vector3.zero in tracking space, so its DT position = translation
        Vector3 xrOriginInDT = computedTranslation;

        Debug.Log("=== [QRManager] TRANSFORMATION COMPUTED ===");
        Debug.Log($"[QRManager] Using markers '{idA}' and '{idB}'");
        Debug.Log($"[QRManager] Rotation: {computedRotation.eulerAngles}");
        Debug.Log($"[QRManager] Translation: {computedTranslation}");
        Debug.Log($"[QRManager] XR Origin in digital twin space: {xrOriginInDT}");
        Debug.Log("=== [QRManager] =============================== ===");

        // Verification: transform both markers and check error
        VerifyTransformation(idA, realA, dtA);
        VerifyTransformation(idB, realB, dtB);
    }

    void VerifyTransformation(string id, Vector3 realPos, Vector3 expectedDTPos)
    {
        Vector3 computedDTPos = computedRotation * realPos + computedTranslation;
        float error = Vector3.Distance(computedDTPos, expectedDTPos);
        Debug.Log($"[QRManager] Verification '{id}': computed={computedDTPos}, expected={expectedDTPos}, error={error:F4}m");
    }

    // --- Public utility for other scripts ---

    /// <summary>
    /// Converts a tracking-space position to digital twin space.
    /// Returns false if the transformation has not been computed yet.
    /// </summary>
    public bool TryTransformToDigitalTwin(Vector3 trackingPos, out Vector3 digitalTwinPos)
    {
        if (!transformComputed)
        {
            digitalTwinPos = Vector3.zero;
            return false;
        }

        digitalTwinPos = computedRotation * trackingPos + computedTranslation;
        return true;
    }
}
