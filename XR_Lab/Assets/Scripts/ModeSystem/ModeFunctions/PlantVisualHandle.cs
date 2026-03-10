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

    private readonly List<RendererState> rendererStates = new List<RendererState>();
    private readonly List<ColliderState> colliderStates = new List<ColliderState>();
    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private GameObject spawnedOverlay;
    private readonly List<GameObject> spawnedIcons = new List<GameObject>();
    private bool originalRenderersHidden;
    private bool initialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (initialized)
            return;

        rendererStates.Clear();
        colliderStates.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial == null)
                continue;

            RendererState state = new RendererState
            {
                renderer = renderer,
                hasBaseColor = sharedMaterial.HasProperty("_BaseColor"),
                hasColor = sharedMaterial.HasProperty("_Color"),
                originalBaseColor = sharedMaterial.HasProperty("_BaseColor") ? sharedMaterial.GetColor("_BaseColor") : default,
                originalColor = sharedMaterial.HasProperty("_Color") ? sharedMaterial.GetColor("_Color") : default
            };

            rendererStates.Add(state);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            colliderStates.Add(new ColliderState
            {
                collider = collider,
                originalEnabled = collider.enabled
            });
        }

        initialized = true;
    }

    /// <summary>
    /// Makes the plant visible (alpha forced to 1) or restores original opacity.
    /// Useful for modes that need to reveal normally-transparent plants without tinting.
    /// </summary>
    public void SetVisible(bool visible)
    {
        InitializeIfNeeded();

        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer == null)
                continue;

            state.renderer.GetPropertyBlock(propertyBlock);

            if (state.hasBaseColor)
            {
                Color color = visible ? WithFullAlpha(state.originalBaseColor) : state.originalBaseColor;
                propertyBlock.SetColor("_BaseColor", color);
            }

            if (state.hasColor)
            {
                Color color = visible ? WithFullAlpha(state.originalColor) : state.originalColor;
                propertyBlock.SetColor("_Color", color);
            }

            state.renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void SetProtectedVisual(bool isProtected, Color protectedTint, bool disableTouchForProtected)
    {
        InitializeIfNeeded();

        for (int index = 0; index < rendererStates.Count; index++)
        {
            RendererState rendererState = rendererStates[index];
            if (rendererState.renderer == null)
                continue;

            rendererState.renderer.GetPropertyBlock(propertyBlock);

            if (rendererState.hasBaseColor)
                propertyBlock.SetColor("_BaseColor", isProtected ? protectedTint : rendererState.originalBaseColor);

            if (rendererState.hasColor)
                propertyBlock.SetColor("_Color", isProtected ? protectedTint : rendererState.originalColor);

            rendererState.renderer.SetPropertyBlock(propertyBlock);
        }

        if (!disableTouchForProtected)
            return;

        for (int index = 0; index < colliderStates.Count; index++)
        {
            ColliderState colliderState = colliderStates[index];
            if (colliderState.collider == null)
                continue;

            colliderState.collider.enabled = isProtected ? false : colliderState.originalEnabled;
        }
    }

    public void DisableColliders()
    {
        InitializeIfNeeded();

        for (int i = 0; i < colliderStates.Count; i++)
        {
            if (colliderStates[i].collider != null)
                colliderStates[i].collider.enabled = false;
        }
    }

    public void RestoreColliders()
    {
        for (int i = 0; i < colliderStates.Count; i++)
        {
            if (colliderStates[i].collider != null)
                colliderStates[i].collider.enabled = colliderStates[i].originalEnabled;
        }
    }

    /// <summary>
    /// Replaces this plant visually: hides the original renderers and spawns the
    /// given prefab at the same position/rotation/scale with the tint applied.
    /// Destroys any existing replacement first.
    /// </summary>
    public void SpawnOverlay(GameObject prefab, Color tint, bool hideOriginal = true)
    {
        DestroyOverlay();

        if (prefab == null)
            return;

        InitializeIfNeeded();

        originalRenderersHidden = hideOriginal;
        if (hideOriginal)
            SetOriginalRenderersEnabled(false);

        spawnedOverlay = Object.Instantiate(prefab, transform.position, transform.rotation, transform);

        Vector3 scale = prefab.transform.localScale;
        MeshFilter parentMF = GetComponent<MeshFilter>();
        MeshFilter overlayMF = spawnedOverlay.GetComponent<MeshFilter>();
        if (parentMF != null && overlayMF != null
            && parentMF.sharedMesh != null && overlayMF.sharedMesh != null)
        {
            Vector3 parentBounds = parentMF.sharedMesh.bounds.size;
            Vector3 overlayBounds = overlayMF.sharedMesh.bounds.size;
            scale = new Vector3(
                overlayBounds.x > 0 ? scale.x * (parentBounds.x / overlayBounds.x) : scale.x,
                overlayBounds.y > 0 ? scale.y * (parentBounds.y / overlayBounds.y) : scale.y,
                overlayBounds.z > 0 ? scale.z * (parentBounds.z / overlayBounds.z) : scale.z
            );
        }
        spawnedOverlay.transform.localScale = scale;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Renderer[] overlayRenderers = spawnedOverlay.GetComponentsInChildren<Renderer>(true);
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

    public void DestroyOverlay()
    {
        if (spawnedOverlay == null)
            return;

        Object.Destroy(spawnedOverlay);
        spawnedOverlay = null;

        if (originalRenderersHidden)
        {
            SetOriginalRenderersEnabled(true);
            originalRenderersHidden = false;
        }
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

        float worldTopY = transform.position.y;
        foreach (RendererState rs in rendererStates)
        {
            if (rs.renderer == null) continue;
            float top = rs.renderer.bounds.max.y;
            if (top > worldTopY) worldTopY = top;
        }

        Vector3 worldTopPoint = new Vector3(transform.position.x, worldTopY, transform.position.z);
        float localTopY = transform.InverseTransformPoint(worldTopPoint).y;

        GameObject icon = Object.Instantiate(prefab, transform);

        Vector3 parentScale = transform.lossyScale;
        icon.transform.localPosition = new Vector3(
            0f,
            localTopY + (parentScale.y > 0f ? yOffset / parentScale.y : yOffset),
            0f);
        icon.transform.localRotation = Quaternion.identity;
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

    private void SetOriginalRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            if (rendererStates[i].renderer != null)
                rendererStates[i].renderer.enabled = enabled;
        }
    }

    public void ResetVisuals()
    {
        DestroyOverlay();
        DestroyIcon();                          
        SetProtectedVisual(false, default, true);
    }

    /// <summary>
    /// Returns the world-space bottom centre and total height of this plant's renderer bounds.
    /// Used by <see cref="PlantIconLODController"/> to position icons correctly per LOD zone.
    /// Falls back to (transform.position, 0) if no valid renderers are found.
    /// </summary>
    public (Vector3 bottomCentre, float height) GetWorldBounds()
    {
        InitializeIfNeeded();

        if (rendererStates.Count == 0)
            return (transform.position, 0f);

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (RendererState rs in rendererStates)
        {
            if (rs.renderer == null) continue;
            Bounds b = rs.renderer.bounds;
            if (b.min.y < minY) minY = b.min.y;
            if (b.max.y > maxY) maxY = b.max.y;
        }

        // Guard: no valid renderers were found
        if (minY == float.MaxValue)
            return (transform.position, 0f);

        Vector3 bottomCentre = new Vector3(transform.position.x, minY, transform.position.z);
        float   height       = Mathf.Max(0f, maxY - minY);
        return (bottomCentre, height);
    }

    private static Color WithFullAlpha(Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }
}
