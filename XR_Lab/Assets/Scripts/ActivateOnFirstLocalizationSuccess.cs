using UnityEngine;

public class ActivateOnFirstLocalizationSuccess : MonoBehaviour
{
    [SerializeField] private GameObject digitalTwinRoot;
    [SerializeField] private bool disableOnStart = true;

    private bool _hasActivated;

    private void Start()
    {
        if (digitalTwinRoot == null)
        {
            Debug.LogWarning("[Localization] Digital twin root is not assigned.", this);
            return;
        }

        if (disableOnStart)
        {
            digitalTwinRoot.SetActive(false);
        }
    }

    public void OnLocalizationSuccess()
    {
        if (_hasActivated)
        {
            return;
        }

        if (digitalTwinRoot == null)
        {
            Debug.LogWarning("[Localization] Digital twin root is not assigned.", this);
            return;
        }

        digitalTwinRoot.SetActive(true);
        _hasActivated = true;
    }
}
