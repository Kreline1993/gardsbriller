using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRInputHandler : MonoBehaviour
{
    public QRManager qrManager;
    public MeasuringTool measuringTool;
    public XRBaseInteractor leftInteractor;

    private InputDevice leftController;
    private bool wasPrimaryPressed = false;
    private bool wasSecondaryPressed = false;
    private bool wasTriggerPressed = false;

    void Start()
    {
        if (qrManager == null)
        {
            Debug.LogError("[XRInputHandler] QRManager reference is not assigned!");
        }

        if (measuringTool == null)
        {
            Debug.LogError("[XRInputHandler] MeasuringTool reference is not assigned!");
        }

        GetLeftController();

        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomCreated);
            Debug.Log("[XRInputHandler] Subscribed to MRUK room events");
        }
    }



    void OnRoomCreated(MRUKRoom room)
    {
        Debug.Log("[XRInputHandler] Room loaded successfully!");

        var anchors = room.GetComponentsInChildren<MRUKAnchor>();

        foreach (var anchor in anchors)
        {
            if (anchor.Label == MRUKAnchor.SceneLabels.FLOOR)
            {
                MeshFilter meshFilter = anchor.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshCollider mc = anchor.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = anchor.gameObject.AddComponent<MeshCollider>();
                    }
                    mc.sharedMesh = meshFilter.sharedMesh;
                }
                else if (anchor.PlaneRect.HasValue)
                {
                    Vector2 size = anchor.PlaneRect.Value.size;
                    Mesh planeMesh = CreatePlaneMesh(size);

                    MeshFilter mf = anchor.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = planeMesh;

                    MeshCollider mc = anchor.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = planeMesh;
                }
            }
        }
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
        if (!leftController.isValid)
        {
            if (Time.frameCount % 60 == 0)
            {
                GetLeftController();
            }
            return;
        }

        // X button - Toggle debug UI
        if (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPrimaryPressed))
        {
            if (isPrimaryPressed && !wasPrimaryPressed)
            {
                if (qrManager != null)
                {
                    qrManager.ToggleDebugUI();
                }
            }
            wasPrimaryPressed = isPrimaryPressed;
        }

        // In Update() method of XRInputHandler

        // Trigger - Select digital twin using XR Interactor
        if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed))
        {
            if (isTriggerPressed && !wasTriggerPressed)
            {
                Debug.Log("[XRInputHandler] TRIGGER PRESSED!");

                if (measuringTool != null && leftInteractor != null)
                {
                    Debug.Log("[XRInputHandler] Calling SelectDigitalTwinFromInteractor");
                    measuringTool.SelectDigitalTwinFromInteractor(leftInteractor);
                }
                else
                {
                    Debug.LogWarning($"[XRInputHandler] measuringTool is null: {measuringTool == null}, leftInteractor is null: {leftInteractor == null}"); // Add this
                }
            }
            wasTriggerPressed = isTriggerPressed;
        }

        // Y button - Measure using XR Interactor
        if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isSecondaryPressed))
        {
            if (isSecondaryPressed && !wasSecondaryPressed)
            {
                Debug.Log("[XRInputHandler] Y BUTTON PRESSED!");

                if (measuringTool != null && leftInteractor != null)
                {
                    Debug.Log("[XRInputHandler] Calling MeasureAccuracyFromInteractor");
                    measuringTool.MeasureAccuracyFromInteractor(leftInteractor);
                }
                else
                {
                    Debug.LogWarning($"[XRInputHandler] measuringTool is null: {measuringTool == null}, leftInteractor is null: {leftInteractor == null}"); // Add this
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
    }
}