using UnityEngine;
using System.Collections;
using MultiSet; 

public class MultisetUpdater : MonoBehaviour 
{
    [SerializeField] private MapLocalizationManager localizationManager;
    [SerializeField] private float UpdateFrequency = 300f;
    [SerializeField , Tooltip("If enabled, an initial API call is sent immediately upon app start.")] private bool sendInitialApiCall = false;
    [SerializeField , Tooltip("If enabled, automatic updating is started automatically on app start, instead of waiting for a manual trigger.")] private bool enableAutomaticUpdatingOnStart = false;

    private Coroutine _timerRoutine;

    void Start() {
        Debug.Log($"[Multiset Updater] sendInitialApiCall={sendInitialApiCall}, enableAutomaticUpdatingOnStart={enableAutomaticUpdatingOnStart}");
        if (enableAutomaticUpdatingOnStart) {
            StartAutomaticUpdating();
        }
    }

    public void PauseAutomaticUpdating() {
        if (_timerRoutine != null) {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }
    }

    public void StartAutomaticUpdating() {
        if (_timerRoutine == null) {
            _timerRoutine = StartCoroutine(ManualLocalizationRoutine());
        }
    }

    IEnumerator ManualLocalizationRoutine() {
        if (localizationManager == null) {
            Debug.LogError("[Multiset Updater] Localization Timer: Manager is null!");
            yield break;
        }

        // 1.THE IMMEDIATE CALL
        if (sendInitialApiCall) {
            // Wait for the end of the frame to ensure the camera/API are ready
            yield return new WaitForEndOfFrame();
            localizationManager.LocalizeFrame();
            Debug.Log("[Multiset Updater] Initial Localization Sent.");
        }

        // 2. THE REPEATING DELAY
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        
        while (true) {
            yield return wait;
            localizationManager.LocalizeFrame();
            Debug.Log("[Multiset Updater] Scheduled Localization Sent.");
        }
    }

    void OnDisable() {
        PauseAutomaticUpdating();
    }

}