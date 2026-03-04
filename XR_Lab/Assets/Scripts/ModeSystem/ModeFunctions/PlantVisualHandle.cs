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

    public void ResetVisuals()
    {
        SetProtectedVisual(false, default, true);
    }

    private static Color WithFullAlpha(Color color)
    {
        return new Color(color.r, color.g, color.b, 1f);
    }
}
