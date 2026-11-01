using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum CandyAllowedSpawnSizes
{
    Small = 1 << 0,
    Medium = 1 << 1,
    Large = 1 << 2,
    Huge = 1 << 3,
    All = Small | Medium | Large | Huge
}

[DisallowMultipleComponent]
public sealed class Candy : MonoBehaviour
{
    private static readonly List<Candy> activeCandies =
        new List<Candy>();

    private const float SpatialCellSize = 6f;
    private static readonly Dictionary<long, List<Candy>> spatialCells =
        new Dictionary<long, List<Candy>>();

    private long spatialCellKey;
    private bool isSpatiallyRegistered;

    public static IReadOnlyList<Candy> ActiveCandies =>
        activeCandies;

    [Header("Gameplay")]
    [SerializeField, Min(0.01f)]
    private float sugarValue = 1f;

    [SerializeField, Min(0f)]
    private float requiredTornadoSugar;

    [SerializeField]
    private Color color = Color.white;

    [Header("Spawner")]
    [SerializeField]
    [Tooltip(
        "Sizes this prefab may use when spawned by CandySpawner."
    )]
    private CandyAllowedSpawnSizes allowedSpawnSizes =
        CandyAllowedSpawnSizes.All;

    [SerializeField, Range(0.1f, 3f)]
    [Tooltip(
        "Visual scale correction for this prefab. " +
        "Use this when its original mesh is naturally too large or small. " +
        "This does not change its sugar reward or size category."
    )]
    private float spawnScaleMultiplier = 1f;

    private CandyAppearance appearance;
    private bool consumed;
    private float physicalHeight = 1f;
    private float physicalRadius = 0.5f;

    public float SugarValue => sugarValue;
    public float RequiredTornadoSugar => requiredTornadoSugar;
    public float PhysicalHeight => physicalHeight;
    public float PhysicalRadius => physicalRadius;
    public Color Color => color;
    public bool IsConsumed => consumed;
    public bool IsBeingAbsorbed => consumed;
    public float SpawnScaleMultiplier =>
        spawnScaleMultiplier;

    public bool AllowsSpawnSize(int sizeIndex)
    {
        if (sizeIndex < 0 || sizeIndex > 3)
        {
            return false;
        }

        CandyAllowedSpawnSizes size =
            (CandyAllowedSpawnSizes)(1 << sizeIndex);

        return (allowedSpawnSizes & size) != 0;
    }

    public event Action<Candy> Consumed;

    private void Awake()
    {
        appearance =
            GetComponent<CandyAppearance>();

        if (appearance == null)
        {
            appearance =
                GetComponentInChildren<CandyAppearance>(
                    true
                );
        }

        color =
            KawaiiColors.GetClosestPaletteColor(
                color
            );

        ApplyColor();
        RefreshPhysicalHeight();
    }

    private void OnEnable()
    {
        consumed = false;

        if (!activeCandies.Contains(this))
        {
            activeCandies.Add(this);
        }

        RegisterSpatially();

        ApplyColor();
        RefreshPhysicalHeight();
    }

    private void OnDisable()
    {
        activeCandies.Remove(this);
        UnregisterSpatially();
    }

    public static void GetNearbyNonAlloc(
        Vector3 position,
        float radius,
        List<Candy> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        int minimumX = Mathf.FloorToInt((position.x - radius) / SpatialCellSize);
        int maximumX = Mathf.FloorToInt((position.x + radius) / SpatialCellSize);
        int minimumZ = Mathf.FloorToInt((position.z - radius) / SpatialCellSize);
        int maximumZ = Mathf.FloorToInt((position.z + radius) / SpatialCellSize);

        for (int x = minimumX; x <= maximumX; x++)
        {
            for (int z = minimumZ; z <= maximumZ; z++)
            {
                if (!spatialCells.TryGetValue(
                        GetSpatialKey(x, z),
                        out List<Candy> cell))
                {
                    continue;
                }

                for (int i = 0; i < cell.Count; i++)
                {
                    Candy candy = cell[i];

                    if (candy != null && candy.isActiveAndEnabled)
                    {
                        results.Add(candy);
                    }
                }
            }
        }
    }

    private void RegisterSpatially()
    {
        UnregisterSpatially();

        int x = Mathf.FloorToInt(transform.position.x / SpatialCellSize);
        int z = Mathf.FloorToInt(transform.position.z / SpatialCellSize);
        spatialCellKey = GetSpatialKey(x, z);

        if (!spatialCells.TryGetValue(spatialCellKey, out List<Candy> cell))
        {
            cell = new List<Candy>(16);
            spatialCells.Add(spatialCellKey, cell);
        }

        cell.Add(this);
        isSpatiallyRegistered = true;
    }

    private void UnregisterSpatially()
    {
        if (!isSpatiallyRegistered)
        {
            return;
        }

        if (spatialCells.TryGetValue(spatialCellKey, out List<Candy> cell))
        {
            cell.Remove(this);

            if (cell.Count == 0)
            {
                spatialCells.Remove(spatialCellKey);
            }
        }

        isSpatiallyRegistered = false;
    }

    private static long GetSpatialKey(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }

    public bool CanBeEatenBy(
        Tornado tornado)
    {
        if (!isActiveAndEnabled ||
            consumed ||
            tornado == null ||
            tornado.IsDefeated)
        {
            return false;
        }

        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (match != null &&
            !match.IsPlaying)
        {
            return false;
        }

        return
            tornado.Sugar >=
            requiredTornadoSugar &&
            tornado.CanFitObject(
                physicalHeight,
                physicalRadius
            );
    }

    public bool Eat(
        Tornado winner)
    {
        return TryConsume(winner);
    }

    public bool TryConsume(
        Tornado winner)
    {
        if (!CanBeEatenBy(winner))
        {
            return false;
        }

        // Reserve immediately so two tornado scans cannot award one candy.
        consumed = true;
        activeCandies.Remove(this);

        CandySwirlVFX.Spawn(
            this,
            winner
        );

        bool sugarAwarded =
            winner.AddSugar(
                sugarValue,
                color
            );

        if (!sugarAwarded)
        {
            consumed = false;

            if (isActiveAndEnabled &&
                !activeCandies.Contains(this))
            {
                activeCandies.Add(this);
            }

            return false;
        }

        if (!winner.IsAI)
        {
            GameAudioManager.Instance
                ?.PlayCandyPickup();
        }

        TornadoMatchManager.Instance
            ?.ReportCandyCollected(
                winner,
                sugarValue,
                color
            );

        Consumed?.Invoke(this);

        // The spawner normally disables the pooled root from the event above.
        // Standalone candy still needs a safe fallback.
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }

        return true;
    }

    public void SetSugarValue(
        float value)
    {
        sugarValue =
            Mathf.Max(
                0.01f,
                value
            );
    }

    public void SetRequiredTornadoSugar(
        float value)
    {
        requiredTornadoSugar =
            Mathf.Max(
                0f,
                value
            );
    }

    public void SetColor(
        Color newColor)
    {
        color =
            KawaiiColors.GetClosestPaletteColor(
                newColor
            );

        ApplyColor();
    }

    public void RefreshPhysicalHeight()
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        bool found = false;
        float minimum = float.MaxValue;
        float maximum = float.MinValue;
        Bounds combinedBounds = default;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null ||
                !renderer.enabled)
            {
                continue;
            }

            Bounds bounds =
                renderer.bounds;

            if (!found)
            {
                combinedBounds = bounds;
            }
            else
            {
                combinedBounds.Encapsulate(bounds);
            }

            minimum =
                Mathf.Min(
                    minimum,
                    bounds.min.y
                );

            maximum =
                Mathf.Max(
                    maximum,
                    bounds.max.y
                );

            found = true;
        }

        physicalHeight =
            found
                ? Mathf.Max(
                    0.01f,
                    maximum - minimum
                )
                : 1f;

        physicalRadius =
            found
                ? Mathf.Max(
                    0.01f,
                    Mathf.Max(
                        combinedBounds.extents.x,
                        combinedBounds.extents.z
                    )
                )
                : 0.5f;
    }

    public void ResetCandy()
    {
        consumed = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void ApplyColor()
    {
        if (appearance != null)
        {
            appearance.ApplyColor(color);
        }
    }

    private void OnValidate()
    {
        sugarValue =
            Mathf.Max(
                0.01f,
                sugarValue
            );

        spawnScaleMultiplier =
            Mathf.Clamp(
                spawnScaleMultiplier,
                0.1f,
                3f
            );

        requiredTornadoSugar =
            Mathf.Max(
                0f,
                requiredTornadoSugar
            );
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeCandies.Clear();
        spatialCells.Clear();
    }
}