using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

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
    }

    void Update()
    {
        // Refresh controller if needed
        if (!leftController.isValid)
        {
            if (Time.frameCount % 60 == 0) // Check every second
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
                // Get controller position and rotation
                if (leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition))
                {
                    if (leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
                    {
                        Vector3 rayDirection = controllerRotation * Vector3.forward;

                        // Point at a fixed distance (e.g., 10 units forward)
                        Vector3 pointedPosition = controllerPosition + (rayDirection * 10f);

                        Debug.Log($"[XRInputHandler] Pointing at position: {pointedPosition}");
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
            // Skip hand tracking devices
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HandTracking))
                continue;

            // Verify it has the primary button
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