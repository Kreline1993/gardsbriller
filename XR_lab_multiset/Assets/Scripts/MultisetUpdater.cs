using UnityEngine;
using System.Collections;
using MultiSet; 

public class CustomLocalizationTimer : MonoBehaviour 
{
    [SerializeField] private MapLocalizationManager localizationManager;
    [SerializeField] private float myCustomDelay = 45f;

    private Coroutine _timerRoutine;

    void Start() {
        // Start instead of OnEnable to ensure 
        // the Manager has had time to initialize itself.
        _timerRoutine = StartCoroutine(ManualLocalizationRoutine());
    }

    IEnumerator ManualLocalizationRoutine() {
        if (localizationManager == null) {
            Debug.LogError("[Multiset Updater] Localization Timer: Manager is null!");
            yield break;
        }

        // 1. THE IMMEDIATE CALL
        // Wait for the end of the frame to ensure the camera/API are ready
        yield return new WaitForEndOfFrame();
        localizationManager.LocalizeFrame();
        Debug.Log("[Multiset Updater] Initial Localization Sent.");

        // 2. THE REPEATING DELAY
        WaitForSeconds wait = new WaitForSeconds(myCustomDelay);
        
        while (true) {
            yield return wait;
            localizationManager.LocalizeFrame();
            Debug.Log("[Multiset Updater] Scheduled Localization Sent.");
        }
    }

    void OnDisable() {
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
    }
}