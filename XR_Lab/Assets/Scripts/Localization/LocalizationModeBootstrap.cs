using UnityEngine;

namespace XRLab.Localization
{
    // Runs in Awake (before MultisetUpdater.Start) to flip on exactly one
    // localization implementation. Set Script Execution Order to a negative
    // value (e.g. -100) so this Awake runs before any Multiset script's Awake.
    public class LocalizationModeBootstrap : MonoBehaviour
    {
        [SerializeField] private LocalizationMode mode = LocalizationMode.Multiset;

        // MonoBehaviour[] lets the Inspector accept any MonoBehaviour-derived
        // component, so we can drag in SDK and project scripts indiscriminately.
        [SerializeField] private MonoBehaviour[] multisetComponents;
        [SerializeField] private MonoBehaviour[] qrComponents;

        [Header("QR mode visibility override")]
        [Tooltip("Drag the digital twin root GameObject here (the one " +
                 "ActivateOnFirstLocalizationSuccess controls). Only used " +
                 "in QR mode — re-activates the twin since its Awake " +
                 "may have hidden it.")]
        [SerializeField] private GameObject digitalTwinRoot;

        private void Awake()
        {
            bool multisetActive = mode == LocalizationMode.Multiset;

            ApplyEnabled(multisetComponents, multisetActive, "multiset");
            ApplyEnabled(qrComponents, !multisetActive, "qr");

            Debug.Log($"[LocalizationModeBootstrap] Mode = {mode}.");

            if (!multisetActive && digitalTwinRoot != null)
            {
                digitalTwinRoot.SetActive(true);
                Debug.Log($"[LocalizationModeBootstrap] QR mode: forcing " +
                          $"{digitalTwinRoot.name} active.", this);
            }
        }

        private void ApplyEnabled(MonoBehaviour[] components, bool enabledValue, string label)
        {
            if (components == null) return;

            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour c = components[i];
                if (c == null)
                {
                    Debug.LogError($"[LocalizationModeBootstrap] {label}Components[{i}] is empty. " +
                                   "Drag a component into the slot or remove the slot.", this);
                    continue;
                }

                c.enabled = enabledValue;
            }
        }
    }
}
