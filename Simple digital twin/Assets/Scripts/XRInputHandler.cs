using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

public class XRInputHandler : MonoBehaviour
{
    public QRManager qrManager;
    private InputDevice leftController;
    private bool wasPrimaryPressed = false;
    private bool wasSecondaryPressed = false;
    public MeasuringTool measuringTool;

    void Start()
    {
        if (qrManager == null)
        {
            Debug.LogError("[XRInputHandler] QRManager reference is not assigned!");
        }
        GetLeftController();

        // Subscribe to room loaded event
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomCreated);
            Debug.Log("[XRInputHandler] Subscribed to MRUK room events");
        }
        else
        {
            Debug.LogWarning("[XRInputHandler] MRUK.Instance is null - room won't load");
        }
    }

    void OnRoomCreated(MRUKRoom room)
    {
        Debug.Log("[XRInputHandler] ? Room loaded successfully!");

        var anchors = room.GetComponentsInChildren<MRUKAnchor>();
        Debug.Log($"[XRInputHandler] Room has {anchors.Length} anchors");

        foreach (var anchor in anchors)
        {
            if (anchor.Label == MRUKAnchor.SceneLabels.FLOOR)
            {
                Debug.Log("[XRInputHandler] Found FLOOR anchor!");
                Debug.Log($"  Position: {anchor.transform.position}");
                Debug.Log($"  Scale: {anchor.transform.localScale}");

                // Check for mesh
                MeshFilter meshFilter = anchor.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Debug.Log($"  Has mesh with {meshFilter.sharedMesh.vertexCount} vertices");

                    // Add/update mesh collider
                    MeshCollider mc = anchor.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = anchor.gameObject.AddComponent<MeshCollider>();
                    }
                    mc.sharedMesh = meshFilter.sharedMesh;
                    Debug.Log("[XRInputHandler] ? Floor collider set up with mesh!");
                }
                else
                {
                    Debug.LogWarning("[XRInputHandler] Floor anchor has NO MESH! Need to generate plane bounds.");

                    // Use plane bounds to create a simple quad mesh
                    if (anchor.PlaneRect.HasValue)
                    {
                        Vector2 size = anchor.PlaneRect.Value.size;
                        Debug.Log($"  Floor plane size: {size}");

                        // Create a simple plane mesh
                        Mesh planeMesh = CreatePlaneMesh(size);

                        MeshFilter mf = anchor.gameObject.AddComponent<MeshFilter>();
                        mf.sharedMesh = planeMesh;

                        MeshCollider mc = anchor.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = planeMesh;

                        Debug.Log("[XRInputHandler] ? Created floor mesh and collider from plane bounds!");
                    }
                }
            }
        }

        Collider[] colliders = FindObjectsOfType<Collider>();
        Debug.Log($"[XRInputHandler] Total colliders: {colliders.Length}");
    }

    Mesh CreatePlaneMesh(Vector2 size)
    {
        Mesh mesh = new Mesh();

        float halfWidth = size.x / 2f;
        float halfHeight = size.y / 2f;

        Vector3[] vertices = new Vector3[]
        {
        new Vector3(-halfWidth, 0, -halfHeight),
        new Vector3(halfWidth, 0, -halfHeight),
        new Vector3(-halfWidth, 0, halfHeight),
        new Vector3(halfWidth, 0, halfHeight)
        };

        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    void Update()
    {
        // Refresh controller if needed
        if (!leftController.isValid)
        {
            if (Time.frameCount % 60 == 0)
            {
                GetLeftController();
            }
            return;
        }

        // Check for X button (primary button) press
        if (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPrimaryPressed))
        {
            if (isPrimaryPressed && !wasPrimaryPressed)
            {
                if (qrManager != null)
                {
                    qrManager.ToggleDebugUI();
                }
                else
                {
                    Debug.LogError("[XRInputHandler] Cannot toggle debug panel - QRManager is null!");
                }
            }
            wasPrimaryPressed = isPrimaryPressed;
        }

        // Check for Y button press
        if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isSecondaryPressed))
        {
            if (isSecondaryPressed && !wasSecondaryPressed)
            {
                if (leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition))
                {
                    if (leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
                    {
                        Vector3 rayDirection = controllerRotation * Vector3.forward;
                        Ray ray = new Ray(controllerPosition, rayDirection);

                        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance: 100f))
                        {
                            Debug.Log($"[XRInputHandler] Hit: {hit.collider.gameObject.name}");
                            Debug.Log($"[XRInputHandler] Position: {hit.point}");
                            Debug.Log($"[XRInputHandler] Distance: {hit.distance}m");
                        }
                        else
                        {
                            Debug.Log("[XRInputHandler] No surface hit");
                        }
                    }
                }
            }
            wasSecondaryPressed = isSecondaryPressed;
        }
    }

    void GetLeftController()
    {
        var leftControllers = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftControllers);

        foreach (var device in leftControllers)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HandTracking))
                continue;

            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool _))
            {
                leftController = device;
                Debug.Log($"[XRInputHandler] Found left controller: {leftController.name}");
                return;
            }
        }

        if (!leftController.isValid)
        {
            Debug.LogWarning("[XRInputHandler] Left controller not found.");
        }
    }
}