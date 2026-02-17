using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class MeasuringTool : MonoBehaviour
{
    private GameObject selectedDigitalTwin = null;

    public void SelectDigitalTwinFromInteractor(XRBaseInteractor interactor)
    {
        // Just use the interactor's transform to create our own ray
        Transform rayOrigin = interactor.transform;
        if (rayOrigin == null)
        {
            rayOrigin = interactor.transform;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Debug.Log($"[MeasuringTool] Ray from: {ray.origin}, direction: {ray.direction}");

        // Do our own raycast
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        Debug.Log($"[MeasuringTool] Found {hits.Length} hits");

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"[MeasuringTool] Hit: {hitObject.name} at distance {hit.distance}");

            string objectName = hitObject.name.ToLower();
            if (!objectName.Contains("floor") &&
                !objectName.Contains("wall") &&
                !objectName.Contains("ceiling") &&
                !objectName.Contains("screen"))
            {
                selectedDigitalTwin = hitObject;
                Debug.Log($"[MeasuringTool] Selected: {selectedDigitalTwin.name}");
                Debug.Log($"[MeasuringTool] Position: {selectedDigitalTwin.transform.position}");
                Debug.Log($"[MeasuringTool] Now press Y to point at the real-world object location");
                return;
            }
        }

        Debug.Log("[MeasuringTool] No valid object found");
    }

    public void MeasureAccuracyFromInteractor(XRBaseInteractor interactor)
    {
        if (selectedDigitalTwin == null)
        {
            Debug.LogWarning("[MeasuringTool] No digital twin selected! Pull trigger while pointing at digital object first");
            return;
        }

        Transform rayOrigin = interactor.transform;
        if (rayOrigin == null)
        {
            rayOrigin = interactor.transform;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 realWorldPosition = hit.point;
            Vector3 digitalTwinPosition = selectedDigitalTwin.transform.position;

            Vector3 offset = realWorldPosition - digitalTwinPosition;
            float distance = offset.magnitude;

            Debug.Log("[MeasuringTool]========== ACCURACY MEASUREMENT ==========");
            Debug.Log($"[MeasuringTool]Digital Twin: {selectedDigitalTwin.name}");
            Debug.Log($"[MeasuringTool]Digital Position: {digitalTwinPosition}");
            Debug.Log($"[MeasuringTool]Real Position: {realWorldPosition}");
            Debug.Log($"[MeasuringTool]Offset: {offset}");
            Debug.Log($"[MeasuringTool]Distance Error: {distance:F3} meters ({distance * 100:F1} cm)");
            Debug.Log($"[MeasuringTool]X Error: {offset.x:F3}m, Y Error: {offset.y:F3}m, Z Error: {offset.z:F3}m");
            Debug.Log("[MeasuringTool]=========================================");

            DrawAccuracyLine(digitalTwinPosition, realWorldPosition, distance);

            selectedDigitalTwin = null;
        }
        else
        {
            Debug.Log("[MeasuringTool] Not pointing at any surface");
        }
    }

    void DrawAccuracyLine(Vector3 digitalPos, Vector3 realPos, float error)
    {
        Debug.DrawLine(digitalPos, realPos, Color.red, 5f);

        GameObject lineObj = new GameObject("AccuracyLine");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.startWidth = 0.01f;
        line.endWidth = 0.01f;
        line.material = new Material(Shader.Find("Sprites/Default"));

        Color lineColor;
        if (error < 0.05f)
            lineColor = Color.green;
        else if (error < 0.15f)
            lineColor = Color.yellow;
        else
            lineColor = Color.red;

        line.startColor = lineColor;
        line.endColor = lineColor;
        line.positionCount = 2;
        line.SetPosition(0, digitalPos);
        line.SetPosition(1, realPos);

        Destroy(lineObj, 5f);
    }
}