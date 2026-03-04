using UnityEngine;

public class ActivateOnFirstLocalizationSuccess : MonoBehaviour
{
    [SerializeField] private GameObject digitalTwinRoot;
    [SerializeField] private bool disableOnStart = true;

    private bool _hasActivated;

    private void Awake()
    {
        if (digitalTwinRoot == null)
        {
            Debug.LogWarning("[Localization] Digital twin root is not assigned.", this);
            return;
        }

        // Disable in Awake() so child Start() methods don't run yet. When activated later, they'll execute properly.
        if (disableOnStart)
        {
            digitalTwinRoot.SetActive(false);
            Debug.Log("[Localization] Disabled digital twin in Awake: " + digitalTwinRoot.name, this);
        }
    }

    public void OnLocalizationSuccess()
    {
        Debug.Log("[Localization] OnLocalizationSuccess called. Already activated: " + _hasActivated, this);
        
        if (_hasActivated)
        {
            return;
        }

        if (digitalTwinRoot == null)
        {
            Debug.LogWarning("[Localization] Digital twin root is not assigned.", this);
            return;
        }

        Debug.Log("[Localization] Activating digital twin: " + digitalTwinRoot.name, this);
        digitalTwinRoot.SetActive(true);
        _hasActivated = true;
    }
}
