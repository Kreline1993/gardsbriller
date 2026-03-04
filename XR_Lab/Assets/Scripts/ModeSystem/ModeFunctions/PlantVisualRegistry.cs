using System.Collections.Generic;
using UnityEngine;

public class PlantVisualRegistry : MonoBehaviour
{
    [SerializeField] private Transform searchRoot;

    private readonly Dictionary<string, PlantVisualHandle> handlesByPlantId = new Dictionary<string, PlantVisualHandle>();

    public IReadOnlyDictionary<string, PlantVisualHandle> HandlesByPlantId => handlesByPlantId;

    private void Awake()
    {
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        handlesByPlantId.Clear();

        Transform root = searchRoot != null ? searchRoot : transform;
        PlantIdentity[] identities = root.GetComponentsInChildren<PlantIdentity>(true);
        
        Debug.Log($"[PlantVisualRegistry] Searching for plants under '{root.name}'. Found {identities.Length} PlantIdentity components.");
        
        foreach (PlantIdentity identity in identities)
        {
            if (identity == null || string.IsNullOrEmpty(identity.plantId))
                continue;

            PlantVisualHandle handle = identity.GetComponent<PlantVisualHandle>();
            if (handle == null)
                handle = identity.gameObject.AddComponent<PlantVisualHandle>();

            handle.InitializeIfNeeded();
            handlesByPlantId[identity.plantId] = handle;
        }
        
        Debug.Log($"[PlantVisualRegistry] Indexed {handlesByPlantId.Count} plants.");
    }

    public void ApplyProtectedSet(HashSet<string> protectedPlantIds, Color protectedTint, bool disableTouchForProtected)
    {
        if (protectedPlantIds == null)
            protectedPlantIds = new HashSet<string>();

        Debug.Log($"[PlantVisualRegistry] Applying protected set: {protectedPlantIds.Count} protected plants, tint={protectedTint}, disableTouch={disableTouchForProtected}");
        
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            PlantVisualHandle handle = pair.Value;
            if (handle == null)
                continue;

            bool isProtected = protectedPlantIds.Contains(pair.Key);
            handle.SetProtectedVisual(isProtected, protectedTint, disableTouchForProtected);
        }
    }

    public void ResetAll()
    {
        foreach (KeyValuePair<string, PlantVisualHandle> pair in handlesByPlantId)
        {
            if (pair.Value == null)
                continue;

            pair.Value.ResetVisuals();
        }
    }
}
