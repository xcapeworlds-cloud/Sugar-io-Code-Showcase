using UnityEngine;

public enum WorldSizeClass
{
    Small,
    Medium,
    Large,
    Huge
}

[DisallowMultipleComponent]
public sealed class WorldSizeSystem : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static WorldSizeSystem instance;

    public static WorldSizeSystem Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<WorldSizeSystem>();

            return instance;
        }
    }

    // =========================================================
    // SMALL
    // =========================================================

    [Header("Small")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Gameplay size used for Small objects.")]
    private float smallSize = 0.35f;

    [SerializeField, Min(0)]
    [Tooltip("Sugar reward for Small objects.")]
    private int smallSugar = 1;

    // =========================================================
    // MEDIUM
    // =========================================================

    [Header("Medium")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Gameplay size used for Medium objects.")]
    private float mediumSize = 0.50f;

    [SerializeField, Min(0)]
    [Tooltip("Sugar reward for Medium objects.")]
    private int mediumSugar = 2;

    // =========================================================
    // LARGE
    // =========================================================

    [Header("Large")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Gameplay size used for Large objects.")]
    private float largeSize = 0.70f;

    [SerializeField, Min(0)]
    [Tooltip("Sugar reward for Large objects.")]
    private int largeSugar = 4;

    // =========================================================
    // HUGE
    // =========================================================

    [Header("Huge")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Gameplay size used for Huge objects.")]
    private float hugeSize = 0.95f;

    [SerializeField, Min(0)]
    [Tooltip("Sugar reward for Huge objects.")]
    private int hugeSugar = 8;

    // =========================================================
    // DESTROYABLE OBJECTS
    // =========================================================

    [Header("Destroyable Objects")]

    [SerializeField, Min(0.001f)]
    [Tooltip(
        "Converts the real rendered height of a destroyable object " +
        "into the same progression scale used by candy. " +
        "Increase this to make destroyables reach larger tiers sooner."
    )]
    private float destroyableSizeMultiplier = 0.15f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Minimum progression size for destroyable objects.")]
    private float minimumDestroyableProgressionSize = 0.35f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Maximum progression size for destroyable objects.")]
    private float maximumDestroyableProgressionSize = 1.50f;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Debug.LogWarning(
                "More than one WorldSizeSystem exists in this scene. " +
                "Only one should exist.",
                this
            );

            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnValidate()
    {
        smallSize =
            Mathf.Max(
                0.01f,
                smallSize
            );

        mediumSize =
            Mathf.Max(
                smallSize,
                mediumSize
            );

        largeSize =
            Mathf.Max(
                mediumSize,
                largeSize
            );

        hugeSize =
            Mathf.Max(
                largeSize,
                hugeSize
            );

        smallSugar =
            Mathf.Max(
                0,
                smallSugar
            );

        mediumSugar =
            Mathf.Max(
                0,
                mediumSugar
            );

        largeSugar =
            Mathf.Max(
                0,
                largeSugar
            );

        hugeSugar =
            Mathf.Max(
                0,
                hugeSugar
            );

        destroyableSizeMultiplier =
            Mathf.Max(
                0.001f,
                destroyableSizeMultiplier
            );

        minimumDestroyableProgressionSize =
            Mathf.Max(
                0.01f,
                minimumDestroyableProgressionSize
            );

        maximumDestroyableProgressionSize =
            Mathf.Max(
                minimumDestroyableProgressionSize,
                maximumDestroyableProgressionSize
            );
    }

    // =========================================================
    // INTERNAL SIZE
    // =========================================================

    private float GetConfiguredSize(
        WorldSizeClass sizeClass)
    {
        switch (sizeClass)
        {
            case WorldSizeClass.Small:
                return smallSize;

            case WorldSizeClass.Medium:
                return mediumSize;

            case WorldSizeClass.Large:
                return largeSize;

            case WorldSizeClass.Huge:
                return hugeSize;

            default:
                return smallSize;
        }
    }

    // =========================================================
    // INTERNAL SUGAR
    // =========================================================

    private int GetConfiguredSugar(
        WorldSizeClass sizeClass)
    {
        switch (sizeClass)
        {
            case WorldSizeClass.Small:
                return smallSugar;

            case WorldSizeClass.Medium:
                return mediumSugar;

            case WorldSizeClass.Large:
                return largeSugar;

            case WorldSizeClass.Huge:
                return hugeSugar;

            default:
                return smallSugar;
        }
    }

    // =========================================================
    // INTERNAL CLASS FROM SIZE
    // =========================================================

    private WorldSizeClass GetConfiguredClassFromSize(
        float size)
    {
        float smallMediumSplit =
            (smallSize + mediumSize) * 0.5f;

        float mediumLargeSplit =
            (mediumSize + largeSize) * 0.5f;

        float largeHugeSplit =
            (largeSize + hugeSize) * 0.5f;

        if (size < smallMediumSplit)
        {
            return WorldSizeClass.Small;
        }

        if (size < mediumLargeSplit)
        {
            return WorldSizeClass.Medium;
        }

        if (size < largeHugeSplit)
        {
            return WorldSizeClass.Large;
        }

        return WorldSizeClass.Huge;
    }

    // =========================================================
    // SIZE FOR CLASS
    // =========================================================

    public static float GetSizeForClass(
        WorldSizeClass sizeClass)
    {
        if (Instance != null)
        {
            return
                Instance.GetConfiguredSize(
                    sizeClass
                );
        }

        return
            GetFallbackSize(
                sizeClass
            );
    }

    // =========================================================
    // SUGAR FOR CLASS
    // =========================================================

    public static int GetSugarForClass(
        WorldSizeClass sizeClass)
    {
        if (Instance != null)
        {
            return
                Instance.GetConfiguredSugar(
                    sizeClass
                );
        }

        return
            GetFallbackSugar(
                sizeClass
            );
    }

    // =========================================================
    // CLASS FROM SIZE
    // =========================================================

    public static WorldSizeClass GetClassFromSize(
        float size)
    {
        if (Instance != null)
        {
            return
                Instance.GetConfiguredClassFromSize(
                    size
                );
        }

        return
            GetFallbackClassFromSize(
                size
            );
    }

    // =========================================================
    // SUGAR FROM SIZE
    // =========================================================

    public static int GetSugarFromSize(
        float size)
    {
        WorldSizeClass sizeClass =
            GetClassFromSize(
                size
            );

        return
            GetSugarForClass(
                sizeClass
            );
    }

    // =========================================================
    // DESTROYABLE / ENVIRONMENT PROGRESSION SIZE
    // =========================================================

    public static float GetEnvironmentProgressionSize(
        float realWorldHeight)
    {
        if (Instance != null)
        {
            float progressionSize =
                Mathf.Max(
                    0f,
                    realWorldHeight
                ) *
                Instance.destroyableSizeMultiplier;

            return
                Mathf.Clamp(
                    progressionSize,
                    Instance.minimumDestroyableProgressionSize,
                    Instance.maximumDestroyableProgressionSize
                );
        }

        return
            Mathf.Clamp(
                Mathf.Max(
                    0f,
                    realWorldHeight
                ) * 0.15f,
                0.35f,
                1.50f
            );
    }

    // =========================================================
    // ALTERNATE DESTROYABLE NAME
    // =========================================================

    public static float GetDestroyableProgressionSize(
        float realWorldHeight)
    {
        return
            GetEnvironmentProgressionSize(
                realWorldHeight
            );
    }

    // =========================================================
    // ENVIRONMENT CLASS
    // =========================================================

    public static WorldSizeClass GetEnvironmentClass(
        float realWorldHeight)
    {
        float progressionSize =
            GetEnvironmentProgressionSize(
                realWorldHeight
            );

        return
            GetClassFromSize(
                progressionSize
            );
    }

    // =========================================================
    // DESTROYABLE CLASS
    // =========================================================

    public static WorldSizeClass GetDestroyableClass(
        float realWorldHeight)
    {
        return
            GetEnvironmentClass(
                realWorldHeight
            );
    }

    // =========================================================
    // ENVIRONMENT SUGAR
    // =========================================================

    public static int GetEnvironmentSugar(
        float realWorldHeight)
    {
        float progressionSize =
            GetEnvironmentProgressionSize(
                realWorldHeight
            );

        return
            GetSugarFromSize(
                progressionSize
            );
    }

    // =========================================================
    // DESTROYABLE SUGAR
    // =========================================================

    public static int GetDestroyableSugar(
        float realWorldHeight)
    {
        return
            GetEnvironmentSugar(
                realWorldHeight
            );
    }

    // =========================================================
    // COMPATIBILITY HELPERS
    // =========================================================

    public static float SizeFor(
        WorldSizeClass sizeClass)
    {
        return
            GetSizeForClass(
                sizeClass
            );
    }

    public static int SugarFor(
        WorldSizeClass sizeClass)
    {
        return
            GetSugarForClass(
                sizeClass
            );
    }

    public static WorldSizeClass ClassForSize(
        float size)
    {
        return
            GetClassFromSize(
                size
            );
    }

    public static int SugarForSize(
        float size)
    {
        return
            GetSugarFromSize(
                size
            );
    }

    public static float DestroyableProgressionSize(
        float realWorldHeight)
    {
        return
            GetEnvironmentProgressionSize(
                realWorldHeight
            );
    }

    public static WorldSizeClass DestroyableClass(
        float realWorldHeight)
    {
        return
            GetEnvironmentClass(
                realWorldHeight
            );
    }

    public static int DestroyableSugar(
        float realWorldHeight)
    {
        return
            GetEnvironmentSugar(
                realWorldHeight
            );
    }

    // =========================================================
    // FALLBACK SIZE
    // =========================================================

    private static float GetFallbackSize(
        WorldSizeClass sizeClass)
    {
        switch (sizeClass)
        {
            case WorldSizeClass.Small:
                return 0.35f;

            case WorldSizeClass.Medium:
                return 0.50f;

            case WorldSizeClass.Large:
                return 0.70f;

            case WorldSizeClass.Huge:
                return 0.95f;

            default:
                return 0.35f;
        }
    }

    // =========================================================
    // FALLBACK SUGAR
    // =========================================================

    private static int GetFallbackSugar(
        WorldSizeClass sizeClass)
    {
        switch (sizeClass)
        {
            case WorldSizeClass.Small:
                return 1;

            case WorldSizeClass.Medium:
                return 2;

            case WorldSizeClass.Large:
                return 4;

            case WorldSizeClass.Huge:
                return 8;

            default:
                return 1;
        }
    }

    // =========================================================
    // FALLBACK CLASS
    // =========================================================

    private static WorldSizeClass GetFallbackClassFromSize(
        float size)
    {
        if (size < 0.425f)
        {
            return WorldSizeClass.Small;
        }

        if (size < 0.60f)
        {
            return WorldSizeClass.Medium;
        }

        if (size < 0.825f)
        {
            return WorldSizeClass.Large;
        }

        return WorldSizeClass.Huge;
    }
}