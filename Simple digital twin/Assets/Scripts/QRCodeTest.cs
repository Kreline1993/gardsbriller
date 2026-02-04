using Meta.XR.MRUtilityKit;
using UnityEngine;

public class QRCodeTest : MonoBehaviour
{
    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        Debug.Log("Trackable added: " + trackable.TrackableType);
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        Debug.Log("Trackable removed: " + trackable.TrackableType);
    }
}
