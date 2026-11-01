using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class CandyAppearance : MonoBehaviour
{
    public enum AppearanceMode
    {
        PreservePrefab,
        ColorWholeCandy,
        ColorMaterialSlot
    }

    // ==================================================
    // APPEARANCE
    // ==================================================

    [Header("Appearance")]

    [Tooltip(
        "Preserve Prefab = don't recolor anything.\n" +
        "Color Whole Candy = recolor all renderers/materials.\n" +
        "Color Material Slot = recolor only one material slot, useful for peppermint."
    )]
    [SerializeField]
    private AppearanceMode appearanceMode =
        AppearanceMode.PreservePrefab;

    [Header("Material Slot Coloring")]

    [Tooltip(
        "Only used with Color Material Slot. " +
        "0 = first material, 1 = second material, etc."
    )]
    [SerializeField, Min(0)]
    private int materialSlot = 1;

    // ==================================================
    // RUNTIME
    // ==================================================

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock colorProperties;
    private bool initialized;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    // ==================================================
    // INITIALIZATION
    // ==================================================

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        cachedRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        colorProperties =
            new MaterialPropertyBlock();

        // Hundreds of small candy shadows and probe lookups are expensive on
        // mobile and contribute almost nothing to the top-down game view.
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer candyRenderer = cachedRenderers[i];

            if (candyRenderer == null)
            {
                continue;
            }

            candyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            candyRenderer.receiveShadows = false;
            candyRenderer.lightProbeUsage = LightProbeUsage.Off;
            candyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            candyRenderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;

            Material[] shared = candyRenderer.sharedMaterials;

            for (int materialIndex = 0;
                 materialIndex < shared.Length;
                 materialIndex++)
            {
                if (shared[materialIndex] != null)
                {
                    shared[materialIndex].enableInstancing = true;
                }
            }
        }
    }

    // ==================================================
    // COLOR
    // ==================================================

    public void ApplyColor(
        Color color)
    {
        Initialize();

        switch (appearanceMode)
        {
            case AppearanceMode.ColorWholeCandy:

                ColorWholeCandy(
                    color
                );

                break;

            case AppearanceMode.ColorMaterialSlot:

                ColorSpecificMaterialSlot(
                    color
                );

                break;

            case AppearanceMode.PreservePrefab:
            default:

                break;
        }
    }

    // ==================================================
    // WHOLE CANDY
    // ==================================================

    private void ColorWholeCandy(
        Color color)
    {
        if (cachedRenderers == null)
            return;

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer renderer =
                cachedRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials =
                renderer.sharedMaterials;

            for (int m = 0;
                 m < materials.Length;
                 m++)
            {
                ApplyRendererColor(renderer, m, color);
            }
        }
    }

    // ==================================================
    // MATERIAL SLOT
    // ==================================================

    private void ColorSpecificMaterialSlot(
        Color color)
    {
        if (cachedRenderers == null)
            return;

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer renderer =
                cachedRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials =
                renderer.sharedMaterials;

            if (materialSlot >=
                materials.Length)
            {
                continue;
            }

            ApplyRendererColor(renderer, materialSlot, color);
        }
    }

    // ==================================================
    // MATERIAL COLOR
    // ==================================================

    private void ApplyRendererColor(
        Renderer targetRenderer,
        int materialIndex,
        Color color)
    {
        if (targetRenderer == null ||
            colorProperties == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(
            colorProperties,
            materialIndex
        );

        colorProperties.SetColor(BaseColorId, color);
        colorProperties.SetColor(ColorId, color);

        targetRenderer.SetPropertyBlock(
            colorProperties,
            materialIndex
        );

        colorProperties.Clear();
    }

    // ==================================================
    // EDITOR
    // ==================================================

    private void OnValidate()
    {
        materialSlot =
            Mathf.Max(
                0,
                materialSlot
            );
    }
}