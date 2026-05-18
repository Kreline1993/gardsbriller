using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlantVisualHandle : MonoBehaviour
{
    private struct RendererState
    {
        public Renderer renderer;
        public bool hasBaseColor;
        public bool hasColor;
        public Color originalBaseColor;
        public Color originalColor;
    }

    private struct ColliderState
    {
        public Collider collider;
        public bool originalEnabled;
    }

    private readonly List<RendererState> baseRendererStates = new List<RendererState>();
    private readonly List<ColliderState> baseColliderStates = new List<ColliderState>();
    private readonly List<RendererState> activeRendererStates = new List<RendererState>();
    private readonly List<ColliderState> activeColliderStates = new List<ColliderState>();
    private MaterialPropertyBlock propertyBlock;
    private readonly List<GameObject> spawnedIcons = new List<GameObject>();

    private PlantAnchor anchor;
    private Transform baseVisualRoot;
    private GameObject activeInteractable;
    private GameObject spawnedOverlay;
    private Transform overlayParent;

    private GameObject overlayPrefab;
    private Color overlayTint = Color.white;
    private bool overlayHideOriginal = true;

    private bool currentVisible;
    private bool currentProtected;
    private Color currentProtectedTint = Color.white;
    private bool disableTouchForProtected;
    private bool forceCollidersDisabled;
    private bool initialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        DestroyOverlayInstance();
        DestroyIcon();
    }

    public void Configure(PlantAnchor owner, Transform baseVisual)
    {
        anchor = owner;
        baseVisualRoot = baseVisual;
        initialized = false;
        InitializeIfNeeded();
        RefreshPresentation();
    }

    public void AttachInteractable(GameObject interactable)
    {
        activeInteractable = interactable;
        CacheActiveStates();
        RefreshPresentation();
    }

    public void DetachInteractable()
    {
        activeInteractable = null;
        activeRendererStates.Clear();
        activeColliderStates.Clear();
        RefreshPresentation();
    }

    public void InitializeIfNeeded()
    {
        if (initialized)
            return;

        if (anchor == null)
            anchor = GetComponent<PlantAnchor>();

        if (baseVisualRoot == null && anchor != null)
            baseVisualRoot = anchor.BaseVisualRoot;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        CacheBaseStates();
        CacheActiveStates();
        initialized = true;
    }

    public void SetVisible(bool visible)
    {
        currentVisible = visible;
        RefreshPresentation();
    }

    public void SetProtectedVisual(bool isProtected, Color protectedTint, bool disableTouchForProtected)
    {
        currentProtected = isProtected;
        currentProtectedTint = protectedTint;
        this.disableTouchForProtected = disableTouchForProtected;
        RefreshPresentation();
    }

    public void DisableColliders()
    {
        forceCollidersDisabled = true;
        RefreshPresentation();
    }

    public void RestoreColliders()
    {
        forceCollidersDisabled = false;
        RefreshPresentation();
    }

    /// <summary>
    /// Replaces this plant visually: hides the original renderers and spawns the
    /// given prefab at the same position/rotation/scale with the tint applied.
    /// Destroys any existing replacement first.
    /// </summary>
    public void SpawnOverlay(GameObject prefab, Color tint, bool hideOriginal = true)
    {
        overlayPrefab = prefab;
        overlayTint = tint;
        overlayHideOriginal = hideOriginal;
        RefreshPresentation();
    }

    public void DestroyOverlay()
    {
        overlayPrefab = null;
        overlayParent = null;
        DestroyOverlayInstance();
        RefreshPresentation();
    }

    /// <summary>
    /// Spawns an icon prefab above this plant's renderer bounds.
    /// The prefab keeps its own scale; the original plant is not hidden.
    /// </summary>
    public void SpawnIconAbove(GameObject prefab, float yOffset = 0.3f)
    {
        if (prefab == null)
            return;

        InitializeIfNeeded();

        float worldTopY = GetWorldTopY();
        Vector3 worldTopPoint = new Vector3(transform.position.x, worldTopY + yOffset, transform.position.z);

        GameObject icon = Object.Instantiate(prefab, transform);
        icon.transform.localPosition = transform.InverseTransformPoint(worldTopPoint);
        icon.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = transform.lossyScale;
        icon.transform.localScale = new Vector3(
            parentScale.x > 0f ? prefab.transform.localScale.x / parentScale.x : prefab.transform.localScale.x,
            parentScale.y > 0f ? prefab.transform.localScale.y / parentScale.y : prefab.transform.localScale.y,
            parentScale.z > 0f ? prefab.transform.localScale.z / parentScale.z : prefab.transform.localScale.z);

        spawnedIcons.Add(icon);
    }

    public void DestroyIcon()
    {
        foreach (GameObject icon in spawnedIcons)
        {
            if (icon != null)
                Object.Destroy(icon);
        }

        spawnedIcons.Clear();
    }

    public void ResetVisuals()
    {
        currentVisible = false;
        currentProtected = false;
        currentProtectedTint = Color.white;
        disableTouchForProtected = false;
        forceCollidersDisabled = false;
        overlayPrefab = null;
        overlayParent = null;
        DestroyOverlayInstance();
        DestroyIcon();
        RefreshPresentation();
    }

    /// <summary>
    /// Returns the world-space bottom centre and total height of this plant's renderer bounds.
    /// Falls back to cached anchor bounds when no live renderers are available.
    /// </summary>
    public (Vector3 bottomCentre, float height) GetWorldBounds()
    {
        InitializeIfNeeded();

        if (anchor != null)
            return anchor.GetWorldBounds();

        if (TryGetWorldBoundsFromRenderers(GetCurrentRendererStates(), out Vector3 bottomCentre, out float height))
            return (bottomCentre, height);

        return (transform.position, 0f);
    }

    private void CacheBaseStates()
    {
        baseRendererStates.Clear();
        baseColliderStates.Clear();

        Transform root = baseVisualRoot != null ? baseVisualRoot : transform;
        CacheStates(root, baseRendererStates, baseColliderStates);
    }

    private void CacheActiveStates()
    {
        activeRendererStates.Clear();
        activeColliderStates.Clear();

        if (activeInteractable == null)
            return;

        CacheStates(activeInteractable.transform, activeRendererStates, activeColliderStates);
    }

    private void RefreshPresentation()
    {
        InitializeIfNeeded();

        bool hasActiveInteractable = activeInteractable != null;
        bool hasOverlay = overlayPrefab != null;

        bool hideBaseRenderers = hasActiveInteractable || (hasOverlay && overlayHideOriginal && !hasActiveInteractable);
        bool hideActiveRenderers = hasActiveInteractable && hasOverlay && overlayHideOriginal;

        SetRenderersEnabled(baseRendererStates, !hideBaseRenderers);
        SetRenderersEnabled(activeRendererStates, hasActiveInteractable && !hideActiveRenderers);

        ApplyRendererProperties(baseRendererStates, !hasActiveInteractable && currentProtected, currentProtectedTint, currentVisible);
        ApplyRendererProperties(activeRendererStates, hasActiveInteractable && currentProtected, currentProtectedTint, currentVisible);

        ApplyColliderStates(baseColliderStates, hasActiveInteractable || forceCollidersDisabled || (currentProtected && disableTouchForProtected));
        ApplyColliderStates(activeColliderStates, forceCollidersDisabled || (currentProtected && disableTouchForProtected));

        if (hasOverlay)
            EnsureOverlay();
        else
            DestroyOverlayInstance();
    }

    private void EnsureOverlay()
    {
        Transform target = GetOverlayTarget();
        if (target == null || overlayPrefab == null)
        {
            DestroyOverlayInstance();
            return;
        }

        if (spawnedOverlay != null && overlayParent == target && MatchesExistingOverlayPrefab())
        {
            ApplyOverlayTint(spawnedOverlay, overlayTint);
            return;
        }

        DestroyOverlayInstance();

        spawnedOverlay = Object.Instantiate(overlayPrefab, target);
        spawnedOverlay.transform.localPosition = Vector3.zero;
        spawnedOverlay.transform.localRotation = Quaternion.identity;
        MatchOverlayScale(target, spawnedOverlay.transform);
        ApplyOverlayTint(spawnedOverlay, overlayTint);
        overlayParent = target;
    }

    private Transform GetOverlayTarget()
    {
        if (activeInteractable != null)
            return activeInteractable.transform;

        return baseVisualRoot != null ? baseVisualRoot : transform;
    }

    private bool MatchesExistingOverlayPrefab()
    {
        return spawnedOverlay != null
            && overlayPrefab != null
            && spawnedOverlay.name.StartsWith(overlayPrefab.name, System.StringComparison.Ordinal);
    }

    private void DestroyOverlayInstance()
    {
        if (spawnedOverlay == null)
            return;

        Object.Destroy(spawnedOverlay);
        spawnedOverlay = null;
        overlayParent = null;
    }

    private List<RendererState> GetCurrentRendererStates()
    {
        return activeInteractable != null ? activeRendererStates : baseRendererStates;
    }

    private float GetWorldTopY()
    {
        if (TryGetWorldBoundsFromRenderers(GetCurrentRendererStates(), out Vector3 bottomCentre, out float height))
            return bottomCentre.y + height;

        if (anchor != null)
        {
            (bottomCentre, height) = anchor.GetWorldBounds();
            return bottomCentre.y + height;
        }

        return transform.position.y;
    }

    private static void CacheStates(Transform root, List<RendererState> renderers, List<ColliderState> colliders)
    {
        if (root == null)
            return;

        Renderer[] foundRenderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in foundRenderers)
        {
            if (renderer == null)
                continue;

            Material sharedMaterial = renderer.sharedMaterial;
            renderers.Add(new RendererState
            {
                renderer = renderer,
                hasBaseColor = sharedMaterial != null && sharedMaterial.HasProperty("_BaseColor"),
                hasColor = sharedMaterial != null && sharedMaterial.HasProperty("_Color"),
                originalBaseColor = sharedMaterial != null && sharedMaterial.HasProperty("_BaseColor") ? sharedMaterial.GetColor("_BaseColor") : default,
                originalColor = sharedMaterial != null && sharedMaterial.HasProperty("_Color") ? sharedMaterial.GetColor("_Color") : default
            });
        }

        Collider[] foundColliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in foundColliders)
        {
            if (collider == null)
                continue;

            colliders.Add(new ColliderState
            {
                collider = collider,
                originalEnabled = collider.enabled
            });
        }
    }

    private void ApplyRendererProperties(List<RendererState> rendererStates, bool isProtected, Color protectedTint, bool visible)
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        for (int index = 0; index < rendererStates.Count; index++)
        {
            RendererState rendererState = rendererStates[index];
            if (rendererState.renderer == null)
                continue;

            rendererState.renderer.GetPropertyBlock(propertyBlock);

            if (rendererState.hasBaseColor)
            {
                Color baseColor = isProtected ? protectedTint : rendererState.originalBaseColor;
                propertyBlock.SetColor("_BaseColor", visible ? WithFullAlpha(baseColor) : baseColor);
            }

            if (rendererState.hasColor)
            {
                Color color = isProtected ? protectedTint : rendererState.originalColor;
                propertyBlock.SetColor("_Color", visible ? WithFullAlpha(color) : color);
            }

            rendererState.renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private static void ApplyColliderStates(List<ColliderState> colliderStates, bool disable)
    {
        for (int index = 0; index < colliderStates.Count; index++)
        {
            ColliderState colliderState = colliderStates[index];
            if (colliderState.collider == null)
                continue;

            colliderState.collider.enabled = disable ? false : colliderState.originalEnabled;
        }
    }

    private static void SetRenderersEnabled(List<RendererState> rendererStates, bool enabled)
    {
        for (int index = 0; index < rendererStates.Count; index++)
        {
            if (rendererStates[index].renderer != null)
                rendererStates[index].renderer.enabled = enabled;
        }
    }

    private static bool TryGetWorldBoundsFromRenderers(List<RendererState> rendererStates, out Vector3 bottomCentre, out float height)
    {
        bottomCentre = default;
        height = 0f;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        bool foundRenderer = false;
        for (int index = 0; index < rendererStates.Count; index++)
        {
            Renderer renderer = rendererStates[index].renderer;
            if (renderer == null)
                continue;

            Bounds bounds = renderer.bounds;
            minY = Mathf.Min(minY, bounds.min.y);
            maxY = Mathf.Max(maxY, bounds.max.y);
            minX = Mathf.Min(minX, bounds.min.x);
            maxX = Mathf.Max(maxX, bounds.max.x);
            minZ = Mathf.Min(minZ, bounds.min.z);
            maxZ = Mathf.Max(maxZ, bounds.max.z);
            foundRenderer = true;
        }

        if (!foundRenderer)
            return false;

        bottomCentre = new Vector3((minX + maxX) * 0.5f, minY, (minZ + maxZ) * 0.5f);
        height = Mathf.Max(0f, maxY - minY);
        return true;
    }

    private static void MatchOverlayScale(Transform target, Transform overlay)
    {
        Vector3 scale = overlay.localScale;
        MeshFilter targetMeshFilter = target.GetComponent<MeshFilter>();
        MeshFilter overlayMeshFilter = overlay.GetComponent<MeshFilter>();

        if (targetMeshFilter != null && overlayMeshFilter != null
            && targetMeshFilter.sharedMesh != null && overlayMeshFilter.sharedMesh != null)
        {
            Vector3 targetBounds = targetMeshFilter.sharedMesh.bounds.size;
            Vector3 overlayBounds = overlayMeshFilter.sharedMesh.bounds.size;
            scale = new Vector3(
                overlayBounds.x > 0f ? scale.x * (targetBounds.x / overlayBounds.x) : scale.x,
                overlayBounds.y > 0f ? scale.y * (targetBounds.y / overlayBounds.y) : scale.y,
                overlayBounds.z > 0f ? scale.z * (targetBounds.z / overlayBounds.z) : scale.z);
        }

        overlay.localScale = scale;
    }

    private static void ApplyOverlayTint(GameObject overlay, Color tint)
    {
        if (overlay == null)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Renderer[] overlayRenderers = overlay.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in overlayRenderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(block);
            Material mat = renderer.sharedMaterial;
            if (mat != null && mat.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", tint);
            if (mat != null && mat.HasProperty("_Color"))
                block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private static Color WithFullAlpha(Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }
}
