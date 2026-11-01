using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SINGLE SOURCE OF TRUTH FOR GAMEPLAY COLORS.
///
/// Candy and tornadoes use the EXACT colors stored here.
/// No hidden saturation.
/// No brightness boost.
/// No HSV modification.
///
/// The floor may still request a softer background version,
/// but that never changes the actual palette.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class KawaiiColors : MonoBehaviour
{
    public static KawaiiColors Instance
    {
        get;
        private set;
    }

    // ==================================================
    // PALETTE
    // ==================================================

    [SerializeField]
    private Color[] palette =
    {
        new Color(1f, 0.38f, 0.58f),
        new Color(1f, 0.60f, 0.48f),
        new Color(1f, 0.84f, 0.35f),
        new Color(0.45f, 0.92f, 0.65f),
        new Color(0.35f, 0.82f, 1f),
        new Color(0.65f, 0.50f, 1f),
        new Color(1f, 0.48f, 0.82f),
        new Color(0.35f, 0.95f, 0.90f)
    };

    public static int Count =>
        Instance != null &&
        Instance.palette != null
            ? Instance.palette.Length
            : 0;

    public static IReadOnlyList<Color> Palette =>
        Count > 0
            ? Instance.palette
            : System.Array.Empty<Color>();

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Only one KawaiiColors component may be active.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        ValidatePalette();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        ValidatePalette();
    }

    // ==================================================
    // EXACT COLOR
    // ==================================================

    public static Color Get(
        int index)
    {
        if (Count == 0)
        {
            return Color.magenta;
        }

        index =
            ((index % Count) + Count) %
            Count;

        Color color =
            Instance.palette[index];

        color.a = 1f;

        return color;
    }

    public static Color RandomColor()
    {
        if (Count <= 0)
        {
            return Color.magenta;
        }

        return Get(
            Random.Range(
                0,
                Count
            )
        );
    }

    // ==================================================
    // CLOSEST PALETTE COLOR
    // ==================================================

    public static Color GetClosestPaletteColor(
        Color color)
    {
        if (Count == 0)
        {
            color.a = 1f;
            return color;
        }

        int bestIndex = 0;

        float bestDistance =
            float.MaxValue;

        for (int i = 0;
             i < Count;
             i++)
        {
            Color candidate =
                Get(i);

            float r =
                candidate.r -
                color.r;

            float g =
                candidate.g -
                color.g;

            float b =
                candidate.b -
                color.b;

            float distance =
                r * r +
                g * g +
                b * b;

            if (distance >=
                bestDistance)
            {
                continue;
            }

            bestDistance =
                distance;

            bestIndex =
                i;
        }

        return Get(
            bestIndex
        );
    }

    // ==================================================
    // GAMEPLAY COLOR COMPATIBILITY
    // ==================================================

    /*
     * These stay so existing scripts do not break.
     *
     * They DO NOT modify the color anymore.
     */

    public static Color GetCandyColor(
        Color baseColor)
    {
        return GetClosestPaletteColor(
            baseColor
        );
    }

    public static Color GetTornadoColor(
        Color baseColor)
    {
        return GetClosestPaletteColor(
            baseColor
        );
    }

    // ==================================================
    // FLOOR
    // ==================================================

    /*
     * Floor is background presentation, not a gameplay
     * color. It can use a softer version without changing
     * the actual KawaiiColors palette.
     */

    public static Color GetFloorColor(
        Color baseColor)
    {
        baseColor =
            GetClosestPaletteColor(
                baseColor
            );

        Color.RGBToHSV(
            baseColor,
            out float h,
            out float s,
            out float v
        );

        s *= 0.35f;

        v =
            Mathf.Lerp(
                v,
                1f,
                0.70f
            );

        Color result =
            Color.HSVToRGB(
                h,
                Mathf.Clamp01(s),
                Mathf.Clamp01(v)
            );

        result.a = 1f;

        return result;
    }

    // ==================================================
    // VALIDATION
    // ==================================================

    private void ValidatePalette()
    {
        if (palette == null ||
            palette.Length == 0)
        {
            palette =
                new[]
                {
                    Color.magenta,
                    Color.cyan,
                    Color.yellow
                };
        }

        for (int i = 0;
             i < palette.Length;
             i++)
        {
            Color color =
                palette[i];

            color.a = 1f;

            palette[i] = color;
        }
    }
}