using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using PassthroughCameraSamples.MultiObjectDetection;

public class XRInputHandler : MonoBehaviour
{
    public QRManager qrManager;
    public MeasuringTool measuringTool;
    public XRBaseInteractor leftInteractor;
    public DetectionUiMenuManager detectionUiMenuManager;
    [SerializeField] private bool forceSentisPausedOnStart = true;

    private InputDevice leftController;
    private InputDevice rightController;
    private bool wasPrimaryPressed = false;
    private bool wasSecondaryPressed = false;
    private bool wasTriggerPressed = false;
    private bool wasLeftSecondaryPressed = false;

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

        TryResolveDetectionUiMenuManager();

        if (forceSentisPausedOnStart)
        {
            StartCoroutine(ForceSentisPausedAfterStart());
        }

        GetLeftController();
        GetRightController();
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
        // Left controller - X button for debug UI
        if (leftController.isValid)
        {
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
                        Debug.LogWarning($"[XRInputHandler] measuringTool is null: {measuringTool == null}, leftInteractor is null: {leftInteractor == null}");
                    }
                }
                wasTriggerPressed = isTriggerPressed;
            }

            // Y button - Measure using XR Interactor
            if (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isLeftSecondaryPressed))
            {
                if (isLeftSecondaryPressed && !wasLeftSecondaryPressed)
                {
                    Debug.Log("[XRInputHandler] Y BUTTON PRESSED!");

                    if (measuringTool != null && leftInteractor != null)
                    {
                        Debug.Log("[XRInputHandler] Calling MeasureAccuracyFromInteractor");
                        measuringTool.MeasureAccuracyFromInteractor(leftInteractor);
                    }
                    else
                    {
                        Debug.LogWarning($"[XRInputHandler] measuringTool is null: {measuringTool == null}, leftInteractor is null: {leftInteractor == null}");
                    }
                }
                wasLeftSecondaryPressed = isLeftSecondaryPressed;
            }
        }
        else if (Time.frameCount % 60 == 0)
        {
            GetLeftController();
        }

        // Right controller - Y button for SENTIS pause
        if (rightController.isValid)
        {
            if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isSecondaryPressed))
            {
                if (isSecondaryPressed && !wasSecondaryPressed)
                {
                    ToggleSentisPause();
                }
                wasSecondaryPressed = isSecondaryPressed;
            }
        }
        else if (Time.frameCount % 60 == 0)
        {
            GetRightController();
        }
    }

    void ToggleSentisPause()
    {
        if (detectionUiMenuManager != null)
        {
            // Toggle pause state
            detectionUiMenuManager.SetPaused(!detectionUiMenuManager.IsPaused);
            Debug.Log($"[XRInputHandler] SENTIS system {(detectionUiMenuManager.IsPaused ? "paused" : "resumed")}");
        }
        else
        {
            Debug.LogError("[XRInputHandler] Cannot toggle SENTIS pause - DetectionUiMenuManager is null!");
        }
    }

    IEnumerator ForceSentisPausedAfterStart()
    {
        yield return null;

        TryResolveDetectionUiMenuManager();
        if (detectionUiMenuManager != null)
        {
            detectionUiMenuManager.SetPaused(true);
        }
        else
        {
            Debug.LogWarning("[XRInputHandler] Sentis pause requested, but DetectionUiMenuManager was not found.");
        }
    }

    void TryResolveDetectionUiMenuManager()
    {
        if (detectionUiMenuManager != null)
        {
            return;
        }

        detectionUiMenuManager = FindObjectOfType<DetectionUiMenuManager>(true);
        if (detectionUiMenuManager == null)
        {
            Debug.LogWarning("[XRInputHandler] DetectionUiMenuManager reference is not assigned!");
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

    void GetRightController()
    {
        var rightControllers = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightControllers);

        foreach (var device in rightControllers)
        {
            // Skip hand tracking devices
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HandTracking))
                continue;

            // Verify it has the secondary button
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool _))
            {
                rightController = device;
                Debug.Log($"[XRInputHandler] Found right controller: {rightController.name}");
                return;
            }
        }

        if (!rightController.isValid)
        {
            Debug.LogWarning("[XRInputHandler] Right controller not found.");
        }
    }
}