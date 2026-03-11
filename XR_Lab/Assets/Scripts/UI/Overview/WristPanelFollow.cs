using UnityEngine;

public class WristPanelFollow : MonoBehaviour
{
    [Tooltip("The hand/wrist transform from your XR rig to follow.")]
    [SerializeField] private Transform wristTransform;

    [Tooltip("Offset from the wrist in the wrist's local space.")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.05f, -0.1f);

    [Tooltip("Extra rotation applied on top of the wrist orientation (Euler degrees).")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(-90f, 0f, 0f);

    [Tooltip("How quickly the panel follows (higher = snappier). Set to 0 for instant.")]
    [SerializeField] private float followSpeed = 12f;

    private void LateUpdate()
    {
        if (wristTransform == null)
            return;

        Vector3 targetPos = wristTransform.TransformPoint(localOffset);
        Quaternion targetRot = wristTransform.rotation * Quaternion.Euler(rotationOffset);

        if (followSpeed <= 0f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }
}