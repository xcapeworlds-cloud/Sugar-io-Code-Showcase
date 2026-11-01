using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TornadoColors : MonoBehaviour
{
    [System.Serializable]
    public sealed class ColorAmount
    {
        public Color color;
        public float amount;

        public ColorAmount(
            Color color,
            float amount)
        {
            this.color = color;
            this.amount = amount;
        }
    }


    [SerializeField]
    private Color startingColor =
        Color.magenta;


    private readonly List<ColorAmount>
        colors = new();


    public IReadOnlyList<ColorAmount>
        Colors => colors;


    public int Revision
    {
        get;
        private set;
    }


    public Color MainColor =>
        GetMainColor();


    private void Awake()
    {
        startingColor =
            KawaiiColors.GetClosestPaletteColor(
                startingColor
            );

        EnsureInitialized();
    }


    public void SetStartingColor(
        Color color)
    {
        startingColor =
            KawaiiColors.GetClosestPaletteColor(
                color
            );

        colors.Clear();

        colors.Add(
            new ColorAmount(
                startingColor,
                1f
            )
        );

        Revision++;
    }


    public void AddColor(
        Color color,
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        EnsureInitialized();

        color =
            KawaiiColors.GetClosestPaletteColor(
                color
            );

        ColorAmount existing =
            FindColor(
                color
            );

        if (existing != null)
        {
            existing.amount +=
                amount;
        }
        else
        {
            colors.Add(
                new ColorAmount(
                    color,
                    amount
                )
            );
        }

        Revision++;
    }


    public Color GetMainColor()
    {
        EnsureInitialized();

        ColorAmount largest =
            colors[0];

        for (int i = 1;
             i < colors.Count;
             i++)
        {
            if (colors[i].amount >
                largest.amount)
            {
                largest =
                    colors[i];
            }
        }

        return
            largest.color;
    }


    public Color GetTornadoColor()
    {
        return
            KawaiiColors.GetTornadoColor(
                GetMainColor()
            );
    }


    public float GetColorWeight(
        int paletteIndex)
    {
        if (paletteIndex < 0 ||
            paletteIndex >=
            KawaiiColors.Count)
        {
            return 0f;
        }

        Color target =
            KawaiiColors.Get(
                paletteIndex
            );

        ColorAmount found =
            FindColor(
                target
            );

        return
            found != null
                ? found.amount
                : 0f;
    }


    public float GetColorPercentage(
        int paletteIndex)
    {
        float total =
            GetTotalAmount();

        if (total <= 0f)
        {
            return 0f;
        }

        return
            GetColorWeight(
                paletteIndex
            ) /
            total;
    }


    public void GetActiveColors(
        List<Color> outputColors,
        List<float> percentages)
    {
        if (outputColors == null ||
            percentages == null)
        {
            return;
        }

        EnsureInitialized();

        outputColors.Clear();
        percentages.Clear();

        float total =
            GetTotalAmount();

        if (total <= 0f)
        {
            outputColors.Add(
                GetMainColor()
            );

            percentages.Add(
                1f
            );

            return;
        }

        for (int i = 0;
             i < colors.Count;
             i++)
        {
            ColorAmount entry =
                colors[i];

            if (entry == null ||
                entry.amount <= 0f)
            {
                continue;
            }

            outputColors.Add(
                entry.color
            );

            percentages.Add(
                entry.amount /
                total
            );
        }
    }


    public void ResetColors()
    {
        colors.Clear();

        EnsureInitialized();
    }


    private void EnsureInitialized()
    {
        if (colors.Count > 0)
        {
            return;
        }

        startingColor =
            KawaiiColors.GetClosestPaletteColor(
                startingColor
            );

        colors.Add(
            new ColorAmount(
                startingColor,
                1f
            )
        );

        Revision++;
    }


    private ColorAmount FindColor(
        Color color)
    {
        for (int i = 0;
             i < colors.Count;
             i++)
        {
            ColorAmount entry =
                colors[i];

            if (entry != null &&
                SameColor(
                    entry.color,
                    color))
            {
                return entry;
            }
        }

        return null;
    }


    private float GetTotalAmount()
    {
        float total =
            0f;

        for (int i = 0;
             i < colors.Count;
             i++)
        {
            if (colors[i] != null)
            {
                total +=
                    Mathf.Max(
                        0f,
                        colors[i].amount
                    );
            }
        }

        return total;
    }


    private static bool SameColor(
        Color a,
        Color b)
    {
        const float tolerance =
            0.001f;

        return
            Mathf.Abs(
                a.r - b.r
            ) < tolerance &&

            Mathf.Abs(
                a.g - b.g
            ) < tolerance &&

            Mathf.Abs(
                a.b - b.b
            ) < tolerance;
    }
}