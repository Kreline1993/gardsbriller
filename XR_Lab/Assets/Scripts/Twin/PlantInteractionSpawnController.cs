using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlantInteractionSpawnController : MonoBehaviour
{
    [Header("Streaming")]
    [SerializeField, Min(0f)] private float interactionSpawnDistance = 10f;
    [SerializeField, Min(0f)] private float interactionDespawnDistance = 12f;
    [SerializeField, Min(0.05f)] private float interactionUpdateInterval = 0.5f;

    private readonly List<PlantAnchor> anchors = new List<PlantAnchor>();

    private GameObject interactionPrefab;
    private Transform userTransform;
    private float timeSinceLastUpdate = float.MaxValue;

    public void Configure(
        GameObject interactionPrefab,
        IEnumerable<PlantAnchor> plantAnchors,
        float spawnDistance,
        float despawnDistance,
        float updateInterval)
    {
        this.interactionPrefab = interactionPrefab;
        interactionSpawnDistance = Mathf.Max(0f, spawnDistance);
        interactionDespawnDistance = Mathf.Max(interactionSpawnDistance, despawnDistance);
        interactionUpdateInterval = Mathf.Max(0.05f, updateInterval);

        anchors.Clear();
        if (plantAnchors != null)
        {
            foreach (PlantAnchor anchor in plantAnchors)
            {
                if (anchor != null)
                    anchors.Add(anchor);
            }
        }

        timeSinceLastUpdate = float.MaxValue;
        enabled = this.interactionPrefab != null && anchors.Count > 0;
    }

    private void Update()
    {
        if (interactionPrefab == null || anchors.Count == 0)
            return;

        if (userTransform == null)
            userTransform = Camera.main != null ? Camera.main.transform : null;

        if (userTransform == null)
            return;

        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate < interactionUpdateInterval)
            return;

        timeSinceLastUpdate = 0f;
        UpdateStreaming();
    }

    private void UpdateStreaming()
    {
        float spawnDistanceSqr = interactionSpawnDistance * interactionSpawnDistance;
        float despawnDistanceSqr = interactionDespawnDistance * interactionDespawnDistance;
        Vector3 userPosition = userTransform.position;

        foreach (PlantAnchor anchor in anchors)
        {
            if (anchor == null)
                continue;

            float distanceSqr = (userPosition - anchor.transform.position).sqrMagnitude;
            if (!anchor.HasActiveInteractable)
            {
                if (distanceSqr <= spawnDistanceSqr)
                    SpawnInteractable(anchor);

                continue;
            }

            if (distanceSqr >= despawnDistanceSqr)
                DespawnInteractable(anchor);
        }
    }

    private void SpawnInteractable(PlantAnchor anchor)
    {
        if (anchor == null || interactionPrefab == null || anchor.HasActiveInteractable)
            return;

        GameObject interactable = Instantiate(interactionPrefab, anchor.transform);
        interactable.name = $"Trigger_{anchor.Species}_{anchor.PlantId}";
        interactable.transform.localPosition = Vector3.zero;
        interactable.transform.localRotation = Quaternion.identity;
        interactable.transform.localScale = anchor.PlantLocalScale;

        PlantIdentity identity = interactable.GetComponent<PlantIdentity>();
        if (identity != null)
            identity.plantId = anchor.PlantId;

        anchor.AttachInteractable(interactable);
    }

    private void DespawnInteractable(PlantAnchor anchor)
    {
        if (anchor == null || !anchor.HasActiveInteractable)
            return;

        anchor.CloseOpenPanels();
        GameObject interactable = anchor.DetachInteractable();
        if (interactable != null)
            Destroy(interactable);
    }
}
