using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CandySpawner : MonoBehaviour
{
    private enum CandySize
    {
        Small,
        Medium,
        Large,
        Huge
    }


    // =========================================================
    // CANDY PREFABS
    // =========================================================

    [Header("Candy Prefabs")]

    [SerializeField]
    [Tooltip(
        "Put ALL candy prefabs here. " +
        "Prefab choice and size choice are independent."
    )]
    private List<GameObject> candyPrefabs =
        new List<GameObject>();


    // =========================================================
    // PLAYING FIELD
    // =========================================================

    [Header("Playing Field")]

    [SerializeField]
    [Tooltip("Playing Field GameObject.")]
    private GameObject playingFieldObject;

    private PlayingField playingField;


    // =========================================================
    // POPULATION
    // =========================================================

    [Header("Candy Population")]

    [SerializeField, Min(1)]
    [Tooltip(
        "Starting number of candies. " +
        "Eaten candy stays gone for the match."
    )]
    private int candyCount = 300;


    // =========================================================
    // SIZE PERCENTAGES
    // =========================================================

    [Header("Candy Size Percentages")]

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Percentage of spawned candy that should be Small.")]
    private float smallSpawnPercentage = 100f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Percentage of spawned candy that should be Medium.")]
    private float mediumSpawnPercentage = 0f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Percentage of spawned candy that should be Large.")]
    private float largeSpawnPercentage = 0f;

    [SerializeField, Range(0f, 100f)]
    [Tooltip("Percentage of spawned candy that should be Huge.")]
    private float hugeSpawnPercentage = 0f;


    // =========================================================
    // SIZE VALUES
    // =========================================================

    [Header("Candy Sizes")]

    [SerializeField, Range(0.05f, 5f)]
    private float smallSize = 0.35f;

    [SerializeField, Range(0.05f, 5f)]
    private float mediumSize = 0.50f;

    [SerializeField, Range(0.05f, 5f)]
    private float largeSize = 0.70f;

    [SerializeField, Range(0.05f, 5f)]
    private float hugeSize = 0.95f;


    // =========================================================
    // SPAWN APPEARANCE
    // =========================================================

    [Header("Spawn Appearance")]

    [SerializeField, Range(0f, 3f)]
    [Tooltip("Preferred space between candies.")]
    private float candySpacing = 0.65f;

    [SerializeField, Range(0f, 3f)]
    [Tooltip("Larger candy receives extra spacing.")]
    private float sizeSpacingInfluence = 0.35f;

    [SerializeField, Range(0f, 5f)]
    [Tooltip("Preferred clearance around destructible structures.")]
    private float structureClearance = 0.8f;

    [SerializeField, Range(0f, 0.5f)]
    [Tooltip(
        "Small visual variation around the selected size. " +
        "Does not change sugar."
    )]
    private float candySizeVariation = 0.07f;

    [SerializeField, Range(1, 100)]
    private int spawnAttempts = 30;


    // =========================================================
    // POOL
    // =========================================================

    private sealed class PooledCandy
    {
        public GameObject root;

        public Candy candy;

        public Vector3 originalRootScale;

        public Quaternion originalRootRotation;

        public int prefabIndex;

        public CandySize size;

        public bool queued;
    }


    private readonly List<PooledCandy> allCandy =
        new List<PooledCandy>();

    private readonly List<Queue<PooledCandy>> availableCandy =
        new List<Queue<PooledCandy>>();

    private readonly List<Vector3> spawnAnchors =
        new List<Vector3>();

    private int nextSpawnAnchor;


    private int activeCount;

    private bool initialPopulationComplete;

    private readonly List<CandySize> initialSizePlan =
        new List<CandySize>();

    private int initialSizePlanIndex;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ResolvePlayingField();

        BuildPools();
    }


    private void Start()
    {
        ResolvePlayingField();


        if (playingField == null)
        {
            Debug.LogError(
                "CandySpawner could not find PlayingField.",
                this
            );

            return;
        }


        if (!HasValidCandyPrefab())
        {
            Debug.LogError(
                "CandySpawner has no valid candy prefabs. " +
                "Add candy prefabs to the Candy Prefabs list.",
                this
            );

            return;
        }


        BuildSpawnPattern();

        BuildExactSizePlan();

        FillPopulation();


        if (activeCount >= candyCount)
        {
            initialPopulationComplete = true;
        }
    }


    private void Update()
    {
        if (initialPopulationComplete)
        {
            return;
        }


        if (activeCount >= candyCount)
        {
            initialPopulationComplete = true;

            return;
        }


        ResolvePlayingField();


        if (playingField == null)
        {
            return;
        }


        FillPopulation();


        if (activeCount >= candyCount)
        {
            initialPopulationComplete = true;
        }
    }


    private void OnDestroy()
    {
        UnsubscribeAll();
    }


    // =========================================================
    // PLAYING FIELD
    // =========================================================

    private void ResolvePlayingField()
    {
        if (playingFieldObject != null)
        {
            playingField =
                playingFieldObject.GetComponent<PlayingField>();


            if (playingField == null)
            {
                playingField =
                    playingFieldObject
                        .GetComponentInChildren<PlayingField>(
                            true
                        );
            }


            if (playingField != null)
            {
                return;
            }
        }


        if (PlayingField.Instance != null)
        {
            playingField =
                PlayingField.Instance;


            if (playingFieldObject == null)
            {
                playingFieldObject =
                    playingField.gameObject;
            }


            return;
        }


        playingField =
            FindFirstObjectByType<PlayingField>();


        if (playingField != null &&
            playingFieldObject == null)
        {
            playingFieldObject =
                playingField.gameObject;
        }
    }


    // =========================================================
    // VALID PREFABS
    // =========================================================

    private bool HasValidCandyPrefab()
    {
        if (candyPrefabs == null)
        {
            return false;
        }


        for (int i = 0;
             i < candyPrefabs.Count;
             i++)
        {
            GameObject prefab =
                candyPrefabs[i];


            if (prefab != null &&
                FindCandy(prefab) != null)
            {
                return true;
            }
        }


        return false;
    }


    private int GetValidPrefabCount()
    {
        int count = 0;


        if (candyPrefabs == null)
        {
            return count;
        }


        for (int i = 0;
             i < candyPrefabs.Count;
             i++)
        {
            GameObject prefab =
                candyPrefabs[i];


            if (prefab != null &&
                FindCandy(prefab) != null)
            {
                count++;
            }
        }


        return count;
    }


    // =========================================================
    // BUILD POOLS
    // =========================================================

    private void BuildPools()
    {
        UnsubscribeAll();


        allCandy.Clear();

        availableCandy.Clear();

        activeCount = 0;

        initialPopulationComplete = false;


        if (candyPrefabs == null ||
            candyPrefabs.Count == 0)
        {
            return;
        }


        for (int i = 0;
             i < candyPrefabs.Count;
             i++)
        {
            availableCandy.Add(
                new Queue<PooledCandy>()
            );
        }


        int validPrefabCount =
            GetValidPrefabCount();


        if (validPrefabCount <= 0)
        {
            return;
        }


        int amountPerPrefab =
            Mathf.CeilToInt(
                candyCount /
                (float)validPrefabCount
            ) +
            8;


        for (int prefabIndex = 0;
             prefabIndex < candyPrefabs.Count;
             prefabIndex++)
        {
            GameObject prefab =
                candyPrefabs[prefabIndex];


            if (prefab == null)
            {
                continue;
            }


            if (FindCandy(prefab) == null)
            {
                Debug.LogError(
                    "Candy prefab '" +
                    prefab.name +
                    "' has no Candy component.",
                    prefab
                );

                continue;
            }


            for (int i = 0;
                 i < amountPerPrefab;
                 i++)
            {
                CreatePooledCandy(
                    prefab,
                    prefabIndex
                );
            }
        }
    }


    // =========================================================
    // CREATE POOLED CANDY
    // =========================================================

    private void CreatePooledCandy(
        GameObject prefab,
        int prefabIndex)
    {
        if (prefab == null)
        {
            return;
        }


        if (prefabIndex < 0 ||
            prefabIndex >= availableCandy.Count)
        {
            return;
        }


        GameObject instanceRoot =
            Instantiate(
                prefab,
                transform
            );


        if (instanceRoot == null)
        {
            return;
        }


        Candy candy =
            FindCandy(
                instanceRoot
            );


        if (candy == null)
        {
            Debug.LogError(
                "Spawned candy '" +
                prefab.name +
                "' has no Candy component.",
                instanceRoot
            );


            Destroy(
                instanceRoot
            );


            return;
        }


        instanceRoot.name =
            prefab.name +
            " (Pooled)";


        PooledCandy pooled =
            new PooledCandy
            {
                root =
                    instanceRoot,

                candy =
                    candy,

                originalRootScale =
                    instanceRoot.transform.localScale,

                originalRootRotation =
                    instanceRoot.transform.localRotation,

                prefabIndex =
                    prefabIndex,

                size =
                    CandySize.Small,

                queued =
                    true
            };


        candy.Consumed +=
            HandleCandyConsumed;


        instanceRoot.SetActive(
            false
        );


        allCandy.Add(
            pooled
        );


        availableCandy[
            prefabIndex
        ].Enqueue(
            pooled
        );
    }


    // =========================================================
    // FIND CANDY
    // =========================================================

    private static Candy FindCandy(
        GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }


        Candy candy =
            obj.GetComponent<Candy>();


        if (candy != null)
        {
            return candy;
        }


        return
            obj.GetComponentInChildren<Candy>(
                true
            );
    }


    // =========================================================
    // POPULATION
    // =========================================================

    private void FillPopulation()
    {
        if (playingField == null)
        {
            return;
        }

        int missing =
            candyCount -
            activeCount;

        if (missing <= 0)
        {
            return;
        }

        /*
         * PERFORMANCE SAFE:
         *
         * Do not try to create the entire population in one frame.
         * Large populations such as 1,000 candies are spread across
         * multiple frames so spawning does not cause a huge CPU spike.
         */
        int successfulThisFrame = 0;

        int maxSuccessfulSpawnsPerFrame =
            20;

        int attemptsThisFrame =
            Mathf.Min(
                80,
                Mathf.Max(
                    20,
                    missing
                )
            );

        int consecutiveFailures = 0;

        while (activeCount < candyCount &&
               attemptsThisFrame-- > 0 &&
               successfulThisFrame <
               maxSuccessfulSpawnsPerFrame)
        {
            if (SpawnRandomCandy())
            {
                successfulThisFrame++;
                consecutiveFailures = 0;
            }
            else
            {
                consecutiveFailures++;

                if (consecutiveFailures >= 8)
                {
                    break;
                }
            }
        }
    }

    // =========================================================
    // RANDOM CANDY
    // =========================================================

    private bool SpawnRandomCandy()
    {
        if (!initialPopulationComplete &&
            initialSizePlanIndex < initialSizePlan.Count)
        {
            /*
             * Try the next planned size first.
             *
             * If that size currently has no usable prefab/pool,
             * try another remaining planned size instead of
             * permanently blocking the entire population.
             *
             * When another planned size succeeds, swap it into
             * the current slot. This preserves the exact final
             * size counts from BuildExactSizePlan().
             */
            for (int planIndex = initialSizePlanIndex;
                 planIndex < initialSizePlan.Count;
                 planIndex++)
            {
                CandySize plannedSize =
                    initialSizePlan[planIndex];

                if (!HasAvailablePrefabForSize(
                        plannedSize))
                {
                    continue;
                }

                if (!TrySpawnSize(
                        plannedSize))
                {
                    continue;
                }

                if (planIndex != initialSizePlanIndex)
                {
                    CandySize temp =
                        initialSizePlan[
                            initialSizePlanIndex
                        ];

                    initialSizePlan[
                        initialSizePlanIndex
                    ] =
                        initialSizePlan[
                            planIndex
                        ];

                    initialSizePlan[
                        planIndex
                    ] =
                        temp;
                }

                initialSizePlanIndex++;

                return true;
            }

            return false;
        }


        return
            TrySpawnSize(
                ChooseRandomSize()
            );
    }


    private bool TrySpawnSize(
        CandySize size)
    {
        int prefabIndex =
            ChooseRandomAvailablePrefab(
                size
            );


        if (prefabIndex >= 0 &&
            SpawnCandy(
                prefabIndex,
                size))
        {
            return true;
        }


        for (int i = 0;
             i < availableCandy.Count;
             i++)
        {
            Queue<PooledCandy> queue =
                availableCandy[i];


            if (queue == null ||
                queue.Count <= 0 ||
                !IsPrefabAllowedForSize(
                    i,
                    size))
            {
                continue;
            }


            if (SpawnCandy(
                    i,
                    size))
            {
                return true;
            }
        }


        return false;
    }


    private bool HasAvailablePrefabForSize(
        CandySize size)
    {
        for (int i = 0;
             i < availableCandy.Count;
             i++)
        {
            Queue<PooledCandy> queue =
                availableCandy[i];


            if (queue != null &&
                queue.Count > 0 &&
                IsPrefabAllowedForSize(
                    i,
                    size))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // PREFAB SELECTION
    // =========================================================

    private int ChooseRandomAvailablePrefab(
        CandySize size)
    {
        if (availableCandy == null ||
            availableCandy.Count == 0)
        {
            return -1;
        }


        int availableCount = 0;


        for (int i = 0;
             i < availableCandy.Count;
             i++)
        {
            Queue<PooledCandy> queue =
                availableCandy[i];


            if (queue != null &&
                queue.Count > 0 &&
                IsPrefabAllowedForSize(
                    i,
                    size))
            {
                availableCount++;
            }
        }


        if (availableCount <= 0)
        {
            return -1;
        }


        int choice =
            Random.Range(
                0,
                availableCount
            );


        for (int i = 0;
             i < availableCandy.Count;
             i++)
        {
            Queue<PooledCandy> queue =
                availableCandy[i];


            if (queue == null ||
                queue.Count <= 0 ||
                !IsPrefabAllowedForSize(
                    i,
                    size))
            {
                continue;
            }


            if (choice == 0)
            {
                return i;
            }


            choice--;
        }


        return -1;
    }


    private bool IsPrefabAllowedForSize(
        int prefabIndex,
        CandySize size)
    {
        if (prefabIndex < 0 ||
            candyPrefabs == null ||
            prefabIndex >= candyPrefabs.Count)
        {
            return false;
        }


        Candy prefabCandy =
            FindCandy(
                candyPrefabs[prefabIndex]
            );


        return
            prefabCandy != null &&
            prefabCandy.AllowsSpawnSize(
                (int)size
            );
    }


    // =========================================================
    // EXACT INITIAL SIZE PLAN
    // =========================================================

    private void BuildExactSizePlan()
    {
        initialSizePlan.Clear();
        initialSizePlanIndex = 0;

        float small = Mathf.Max(0f, smallSpawnPercentage);
        float medium = Mathf.Max(0f, mediumSpawnPercentage);
        float large = Mathf.Max(0f, largeSpawnPercentage);
        float huge = Mathf.Max(0f, hugeSpawnPercentage);

        float total = small + medium + large + huge;

        if (total <= 0f)
        {
            small = 100f;
            medium = 0f;
            large = 0f;
            huge = 0f;
            total = 100f;
        }

        float[] exact =
        {
            candyCount * (small / total),
            candyCount * (medium / total),
            candyCount * (large / total),
            candyCount * (huge / total)
        };

        int[] counts =
        {
            Mathf.FloorToInt(exact[0]),
            Mathf.FloorToInt(exact[1]),
            Mathf.FloorToInt(exact[2]),
            Mathf.FloorToInt(exact[3])
        };

        int assigned = counts[0] + counts[1] + counts[2] + counts[3];
        int remaining = candyCount - assigned;

        while (remaining > 0)
        {
            int bestIndex = 0;
            float bestRemainder = exact[0] - counts[0];

            for (int i = 1; i < 4; i++)
            {
                float remainder = exact[i] - counts[i];

                if (remainder > bestRemainder)
                {
                    bestRemainder = remainder;
                    bestIndex = i;
                }
            }

            counts[bestIndex]++;
            exact[bestIndex] = counts[bestIndex];
            remaining--;
        }

        AddSizeToPlan(CandySize.Small, counts[0]);
        AddSizeToPlan(CandySize.Medium, counts[1]);
        AddSizeToPlan(CandySize.Large, counts[2]);
        AddSizeToPlan(CandySize.Huge, counts[3]);

        ShuffleSizePlan();

        Debug.Log(
            "Candy size plan: Small=" + counts[0] +
            ", Medium=" + counts[1] +
            ", Large=" + counts[2] +
            ", Huge=" + counts[3],
            this
        );
    }


    private void AddSizeToPlan(CandySize size, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            initialSizePlan.Add(size);
        }
    }


    private void ShuffleSizePlan()
    {
        for (int i = initialSizePlan.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CandySize temp = initialSizePlan[i];
            initialSizePlan[i] = initialSizePlan[randomIndex];
            initialSizePlan[randomIndex] = temp;
        }
    }


    // =========================================================
    // SIZE SELECTION
    // =========================================================

    private CandySize ChooseRandomSize()
    {
        float small =
            GetAvailableSizePercentage(
                CandySize.Small,
                smallSpawnPercentage
            );

        float medium =
            GetAvailableSizePercentage(
                CandySize.Medium,
                mediumSpawnPercentage
            );

        float large =
            GetAvailableSizePercentage(
                CandySize.Large,
                largeSpawnPercentage
            );

        float huge =
            GetAvailableSizePercentage(
                CandySize.Huge,
                hugeSpawnPercentage
            );


        float total =
            small +
            medium +
            large +
            huge;


        if (total <= 0f)
        {
            return CandySize.Small;
        }


        /*
         * Normalize all configured values.
         *
         * Example:
         *
         * Small  = 97
         * Medium = 1
         * Large  = 1
         * Huge   = 1
         *
         * Gives:
         *
         * Small  = 97%
         * Medium = 1%
         * Large  = 1%
         * Huge   = 1%
         */
        float smallChance =
            small / total;

        float mediumChance =
            medium / total;

        float largeChance =
            large / total;


        float roll =
            Random.value;


        if (roll < smallChance)
        {
            return CandySize.Small;
        }


        roll -=
            smallChance;


        if (roll < mediumChance)
        {
            return CandySize.Medium;
        }


        roll -=
            mediumChance;


        if (roll < largeChance)
        {
            return CandySize.Large;
        }


        return CandySize.Huge;
    }


    private float GetAvailableSizePercentage(
        CandySize size,
        float configuredPercentage)
    {
        if (configuredPercentage <= 0f)
        {
            return 0f;
        }


        for (int i = 0;
             i < availableCandy.Count;
             i++)
        {
            Queue<PooledCandy> queue =
                availableCandy[i];


            if (queue != null &&
                queue.Count > 0 &&
                IsPrefabAllowedForSize(
                    i,
                    size))
            {
                return configuredPercentage;
            }
        }


        return 0f;
    }


    // =========================================================
    // SPAWN
    // =========================================================

    private bool SpawnCandy(
        int prefabIndex,
        CandySize size)
    {
        if (prefabIndex < 0 ||
            prefabIndex >= availableCandy.Count)
        {
            return false;
        }


        Queue<PooledCandy> queue =
            availableCandy[
                prefabIndex
            ];


        if (queue == null ||
            queue.Count == 0)
        {
            return false;
        }


        PooledCandy pooled =
            queue.Dequeue();


        pooled.queued =
            false;


        float classSize =
            GetSize(
                size
            );


        int sugar =
            GetSugarForSize(
                size
            );


        float visualSize =
            ApplySizeVariation(
                classSize
            );


        if (!TryGetPrettySpawnPosition(
                visualSize,
                out Vector3 position))
        {
            ReturnToPool(
                pooled
            );


            return false;
        }


        pooled.size =
            size;


        PrepareTransform(
            pooled,
            position
        );


        Candy prefabCandy =
            FindCandy(
                candyPrefabs[prefabIndex]
            );


        float prefabScaleMultiplier =
            prefabCandy != null
                ? prefabCandy.SpawnScaleMultiplier
                : 1f;


        pooled.root.transform.localScale =
            pooled.originalRootScale *
            visualSize *
            prefabScaleMultiplier;


        pooled.root.name =
            candyPrefabs[prefabIndex].name +
            " [" +
            size +
            "]";


        pooled.candy.SetSugarValue(
            sugar
        );


        pooled.candy.SetColor(
            GetRandomCandyColor()
        );


        ActivatePooledCandy(
            pooled
        );


        pooled.candy.RefreshPhysicalHeight();


        activeCount++;


        return true;
    }


    // =========================================================
    // SIZE VALUES
    // =========================================================

    private float GetSize(
        CandySize size)
    {
        switch (size)
        {
            case CandySize.Medium:
                return mediumSize;


            case CandySize.Large:
                return largeSize;


            case CandySize.Huge:
                return hugeSize;


            case CandySize.Small:
            default:
                return smallSize;
        }
    }


    // =========================================================
    // SUGAR
    // =========================================================

    private static int GetSugarForSize(
        CandySize size)
    {
        WorldSizeClass sizeClass;

        switch (size)
        {
            case CandySize.Medium:
                sizeClass = WorldSizeClass.Medium;
                break;

            case CandySize.Large:
                sizeClass = WorldSizeClass.Large;
                break;

            case CandySize.Huge:
                sizeClass = WorldSizeClass.Huge;
                break;

            case CandySize.Small:
            default:
                sizeClass = WorldSizeClass.Small;
                break;
        }

        return WorldSizeSystem.GetSugarForClass(sizeClass);
    }


    // =========================================================
    // VISUAL VARIATION
    // =========================================================

    private float ApplySizeVariation(
        float baseSize)
    {
        float variation =
            Random.Range(
                1f - candySizeVariation,
                1f + candySizeVariation
            );


        return
            Mathf.Max(
                0.05f,
                baseSize *
                variation
            );
    }


    // =========================================================
    // SPAWN POSITION
    // =========================================================

    private void BuildSpawnPattern()
    {
        spawnAnchors.Clear();
        nextSpawnAnchor = 0;
        if (playingField == null) return;

        Bounds bounds = playingField.PlayableBounds;
        float width = Mathf.Max(0.01f, bounds.size.x);
        float depth = Mathf.Max(0.01f, bounds.size.z);
        // Extra grid cells provide alternatives when objects block a cell.
        int cells = Mathf.Max(1, candyCount * 3);
        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(cells * width / depth)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(cells / (float)columns));

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                spawnAnchors.Add(new Vector3(
                    bounds.min.x + (x + 0.5f) * width / columns,
                    bounds.center.y,
                    bounds.min.z + (z + 0.5f) * depth / rows));
            }
        }

        // Shuffle grid cells so candies appear evenly over the entire map
        // even while population is being spawned across multiple frames.
        for (int i = spawnAnchors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 temp = spawnAnchors[i];
            spawnAnchors[i] = spawnAnchors[j];
            spawnAnchors[j] = temp;
        }
    }

    private bool TryGetPrettySpawnPosition(float candySize, out Vector3 position)
    {
        position = transform.position;
        if (playingField == null) return false;
        if (spawnAnchors.Count == 0) BuildSpawnPattern();

        // Each cell is tested at most once; never force a spawn into an object.
        int attempts = Mathf.Min(spawnAttempts, spawnAnchors.Count);
        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = spawnAnchors[nextSpawnAnchor];
            nextSpawnAnchor = (nextSpawnAnchor + 1) % spawnAnchors.Count;
            candidate = playingField.ClampPosition(candidate);

            if (!IsStructurePositionClear(candidate, candySize)) continue;
            if (!IsObjectPositionClear(candidate, candySize)) continue;

            position = candidate;
            return true;
        }
        return false;
    }

    private bool IsObjectPositionClear(Vector3 position, float candySize)
    {
        float radius = Mathf.Max(0.1f, candySize * 0.5f + structureClearance);
        // Check around the candy, not below the ground plane.
        Vector3 center = position + Vector3.up * radius;
        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            // The playing field itself is not an obstacle.
            if (playingField != null && hit.transform.IsChildOf(playingField.transform)) continue;
            // Ignore existing candy: grid spacing already distributes the spawns.
            if (hit.GetComponentInParent<Candy>() != null) continue;
            // Ignore this spawner's own hierarchy.
            if (hit.transform.IsChildOf(transform)) continue;
            return false;
        }
        return true;
    }

    private bool IsStructurePositionClear(Vector3 position, float candySize)
    {
        float radius = Mathf.Max(0.1f, structureClearance + candySize * 0.5f);
        IReadOnlyList<TornadoDestructible> destructibles = TornadoDestructible.ActiveDestructibles;
        for (int i = 0; i < destructibles.Count; i++)
        {
            TornadoDestructible destructible = destructibles[i];
            if (destructible != null && destructible.isActiveAndEnabled &&
                destructible.BlocksCandySpawn(position, radius)) return false;
        }
        return true;
    }

    // =========================================================
    // TRANSFORM / ROTATION
    // =========================================================

    private void PrepareTransform(
        PooledCandy pooled,
        Vector3 position)
    {
        if (pooled == null ||
            pooled.root == null)
        {
            return;
        }


        Transform t =
            pooled.root.transform;


        t.SetParent(
            transform,
            false
        );


        t.position =
            position;


        Vector3 originalEuler =
            pooled.originalRootRotation.eulerAngles;


        float randomY =
            Random.Range(
                0f,
                360f
            );


        t.localRotation =
            Quaternion.Euler(
                originalEuler.x,
                originalEuler.y + randomY,
                originalEuler.z
            );


        t.localScale =
            pooled.originalRootScale;
    }


    // =========================================================
    // COLOR
    // =========================================================

    private Color GetRandomCandyColor()
    {
        return
            KawaiiColors.RandomColor();
    }


    // =========================================================
    // ACTIVATE
    // =========================================================

    private void ActivatePooledCandy(
        PooledCandy pooled)
    {
        if (pooled == null ||
            pooled.root == null ||
            pooled.candy == null)
        {
            return;
        }


        pooled.root.SetActive(
            true
        );


        if (!pooled.candy.gameObject.activeSelf)
        {
            pooled.candy.gameObject.SetActive(
                true
            );
        }
    }


    // =========================================================
    // CONSUMED
    // =========================================================

    private void HandleCandyConsumed(
        Candy candy)
    {
        PooledCandy pooled =
            FindPooledCandy(
                candy
            );


        if (pooled == null)
        {
            return;
        }


        activeCount =
            Mathf.Max(
                0,
                activeCount - 1
            );


        ReturnToPool(
            pooled
        );
    }


    private PooledCandy FindPooledCandy(
        Candy candy)
    {
        if (candy == null)
        {
            return null;
        }


        for (int i = 0;
             i < allCandy.Count;
             i++)
        {
            PooledCandy pooled =
                allCandy[i];


            if (pooled != null &&
                pooled.candy == candy)
            {
                return pooled;
            }
        }


        return null;
    }


    // =========================================================
    // RETURN TO POOL
    // =========================================================

    private void ReturnToPool(
        PooledCandy pooled)
    {
        if (pooled == null ||
            pooled.queued)
        {
            return;
        }


        pooled.queued =
            true;


        if (pooled.root != null)
        {
            pooled.root.SetActive(
                false
            );


            if (pooled.prefabIndex >= 0 &&
                pooled.prefabIndex < candyPrefabs.Count &&
                candyPrefabs[pooled.prefabIndex] != null)
            {
                pooled.root.name =
                    candyPrefabs[pooled.prefabIndex].name +
                    " (Pooled)";
            }


            pooled.root.transform.localScale =
                pooled.originalRootScale;


            pooled.root.transform.localRotation =
                pooled.originalRootRotation;
        }


        int prefabIndex =
            pooled.prefabIndex;


        if (prefabIndex >= 0 &&
            prefabIndex < availableCandy.Count)
        {
            availableCandy[
                prefabIndex
            ].Enqueue(
                pooled
            );
        }
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void UnsubscribeAll()
    {
        for (int i = 0;
             i < allCandy.Count;
             i++)
        {
            PooledCandy pooled =
                allCandy[i];


            if (pooled != null &&
                pooled.candy != null)
            {
                pooled.candy.Consumed -=
                    HandleCandyConsumed;
            }
        }
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        candyCount =
            Mathf.Max(
                1,
                candyCount
            );





        smallSpawnPercentage =
            Mathf.Clamp(
                smallSpawnPercentage,
                0f,
                100f
            );


        mediumSpawnPercentage =
            Mathf.Clamp(
                mediumSpawnPercentage,
                0f,
                100f
            );


        largeSpawnPercentage =
            Mathf.Clamp(
                largeSpawnPercentage,
                0f,
                100f
            );


        hugeSpawnPercentage =
            Mathf.Clamp(
                hugeSpawnPercentage,
                0f,
                100f
            );


        smallSize =
            Mathf.Clamp(
                smallSize,
                0.05f,
                5f
            );


        mediumSize =
            Mathf.Clamp(
                mediumSize,
                0.05f,
                5f
            );


        largeSize =
            Mathf.Clamp(
                largeSize,
                0.05f,
                5f
            );


        hugeSize =
            Mathf.Clamp(
                hugeSize,
                0.05f,
                5f
            );


        candySpacing =
            Mathf.Clamp(
                candySpacing,
                0f,
                3f
            );


        sizeSpacingInfluence =
            Mathf.Clamp(
                sizeSpacingInfluence,
                0f,
                3f
            );


        structureClearance =
            Mathf.Clamp(
                structureClearance,
                0f,
                5f
            );


        candySizeVariation =
            Mathf.Clamp(
                candySizeVariation,
                0f,
                0.5f
            );


        spawnAttempts =
            Mathf.Clamp(
                spawnAttempts,
                1,
                100
            );


        if (candyPrefabs == null)
        {
            candyPrefabs =
                new List<GameObject>();
        }
    }
}