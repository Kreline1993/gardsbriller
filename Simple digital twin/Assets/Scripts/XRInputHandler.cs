using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class XRInputHandler : MonoBehaviour
{
    public QRManager qrManager;

    private InputDevice leftController;
    private bool wasPrimaryPressed = false;

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