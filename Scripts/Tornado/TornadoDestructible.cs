using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TornadoDestructible : MonoBehaviour
{
    private static readonly List<TornadoDestructible>
        activeDestructibles = new();

    public static IReadOnlyList<TornadoDestructible>
        ActiveDestructibles =>
            activeDestructibles;


    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]

    [SerializeField]
    [Tooltip(
        "Friendly name shown when the player destroys/eats this object. " +
        "Leave empty to automatically use the GameObject name."
    )]
    private string displayName = "";


    // =========================================================
    // PROGRESSION
    // =========================================================

    [Header("Progression")]

    [SerializeField]
    [Tooltip(
        "ON = Size Requirement is automatically calculated " +
        "from the object's actual rendered height.\n\n" +
        "OFF = use the Size Requirement entered below."
    )]
    private bool automaticSizeRequirement = true;


    [SerializeField, Min(0.1f)]
    [Tooltip(
        "How tall the tornado must be to destroy this object."
    )]
    private float sizeRequirement = 1f;


    [SerializeField, Min(0f)]
    [Tooltip(
        "Exact sugar awarded when this object is consumed."
    )]
    private float sugarValue = 1f;

    [Header("Audio")]

    [SerializeField]
    [Tooltip(
        "Optional sound for this object. Leave empty to use " +
        "GameAudioManager's Default Destruction Clip."
    )]
    private AudioClip destructionSoundOverride;


    // =========================================================
    // AUTOMATIC SIZE
    // =========================================================

    [Header("Automatic Size")]

    [SerializeField, Min(0.01f)]
    private float automaticSizeMultiplier = 1f;

    [SerializeField]
    private float automaticSizeOffset = 0f;


    // =========================================================
    // RUNTIME
    // =========================================================

    private Renderer[] cachedRenderers;

    private readonly List<Transform>
        pieces = new();

    private readonly HashSet<Transform>
        consumedPieces = new();

    private float objectHeight = 1f;
    private float objectRadius = 0.5f;
    private float calculatedSizeRequirement = 1f;

    private bool initialized;
    private bool activated;
    private bool sugarClaimed;


    // =========================================================
    // PUBLIC VALUES
    // =========================================================

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    displayName))
            {
                return displayName.Trim();
            }

            string fallback =
                gameObject.name;

            fallback =
                fallback.Replace(
                    "(Clone)",
                    string.Empty
                );

            fallback =
                fallback.Replace(
                    "_",
                    " "
                );

            return fallback.Trim();
        }
    }


    public float SugarValue =>
        sugarValue;

    public AudioClip DestructionSoundOverride =>
        destructionSoundOverride;

    public float RemainingSugar =>
        sugarClaimed
            ? 0f
            : Mathf.Max(0f, sugarValue);

    public float ObjectOverallSize =>
        objectHeight;

    public float ObjectHeight =>
        objectHeight;

    public float ObjectRadius =>
        objectRadius;

    public bool IsActivated =>
        activated;

    public bool IsFullyConsumed =>
        pieces.Count > 0 &&
        consumedPieces.Count >= pieces.Count;

    public bool IsWaitingForRespawn =>
        false;

    public bool AutomaticSizeRequirement =>
        automaticSizeRequirement;

    public float BreakRequirement =>
        calculatedSizeRequirement;

    public float SizeRequirement =>
        calculatedSizeRequirement;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Initialize();
    }


    private void OnEnable()
    {
        if (!activeDestructibles.Contains(this))
        {
            activeDestructibles.Add(this);
        }

        if (!initialized)
        {
            Initialize();
        }
    }


    private void OnDisable()
    {
        activeDestructibles.Remove(this);
    }


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Initialize()
    {
        initialized = false;
        activated = false;
        sugarClaimed = false;

        pieces.Clear();
        consumedPieces.Clear();

        RefreshCache();

        CalculateObjectHeight();
        CalculateSizeRequirement();

        FindActualPieces();

        initialized = true;
    }


    // =========================================================
    // OBJECT HEIGHT
    // =========================================================

    private void CalculateObjectHeight()
    {
        Renderer[] renderers =
            GetCachedRenderers();

        if (renderers == null ||
            renderers.Length == 0)
        {
            objectHeight = 1f;
            objectRadius = 0.5f;
            return;
        }

        float minY =
            float.MaxValue;

        float maxY =
            float.MinValue;

        bool found =
            false;

        Bounds combinedBounds = default;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
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

            minY =
                Mathf.Min(
                    minY,
                    bounds.min.y
                );

            maxY =
                Mathf.Max(
                    maxY,
                    bounds.max.y
                );

            found = true;
        }

        if (!found)
        {
            objectHeight = 1f;
            objectRadius = 0.5f;
            return;
        }

        objectHeight =
            Mathf.Max(
                0.1f,
                maxY - minY
            );

        objectRadius =
            Mathf.Max(
                0.05f,
                Mathf.Max(
                    combinedBounds.extents.x,
                    combinedBounds.extents.z
                )
            );
    }


    // =========================================================
    // SIZE REQUIREMENT
    // =========================================================

    private void CalculateSizeRequirement()
    {
        if (automaticSizeRequirement)
        {
            calculatedSizeRequirement =
                Mathf.Max(
                    0.1f,
                    objectHeight *
                    automaticSizeMultiplier +
                    automaticSizeOffset
                );

            return;
        }

        calculatedSizeRequirement =
            Mathf.Max(
                0.1f,
                sizeRequirement
            );
    }


    // =========================================================
    // TORNADO SIZE
    // =========================================================

    public float GetTornadoOverallSize(
        Tornado tornado)
    {
        if (tornado == null)
        {
            return 0f;
        }

        return tornado.GetVisibleHeight();
    }


    public float GetSizeRatio(
        Tornado tornado)
    {
        if (tornado == null)
        {
            return 0f;
        }

        return
            tornado.GetVisibleHeight() /
            Mathf.Max(
                0.1f,
                calculatedSizeRequirement
            );
    }


    public bool MeetsSugarRequirement(
        Tornado tornado)
    {
        return IsTornadoBigEnough(
            tornado
        );
    }


    public bool IsTornadoBigEnough(
        Tornado tornado)
    {
        if (tornado == null ||
            tornado.IsDefeated)
        {
            return false;
        }

        return tornado.CanFitObject(
            calculatedSizeRequirement,
            objectRadius
        );
    }


    // =========================================================
    // ACTIVATION
    // =========================================================

    public bool CanActivate(
        Tornado tornado)
    {
        if (activated)
        {
            return true;
        }

        if (tornado == null ||
            tornado.IsDefeated)
        {
            return false;
        }

        return IsTornadoBigEnough(
            tornado
        );
    }


    public bool TryActivate(
        Tornado tornado)
    {
        if (activated)
        {
            return true;
        }

        if (!CanActivate(tornado))
        {
            return false;
        }

        if (pieces.Count == 0)
        {
            return false;
        }

        activated = true;

        PreparePiecesForBreak();

        return true;
    }


    // =========================================================
    // FIND PIECES
    // =========================================================

    private void FindActualPieces()
    {
        /*
         * If the root itself has a Renderer,
         * use the whole root as one piece.
         */
        if (GetComponent<Renderer>() != null)
        {
            AddPiece(
                transform
            );

            return;
        }

        /*
         * Otherwise find rendered children.
         */
        for (int i = 0;
             i < transform.childCount;
             i++)
        {
            CollectPiecesRecursive(
                transform.GetChild(i)
            );
        }
    }


    private void CollectPiecesRecursive(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        TornadoDestructible nested =
            target.GetComponent<TornadoDestructible>();

        if (nested != null && nested != this)
        {
            return;
        }

        Renderer renderer =
            target.GetComponent<Renderer>();

        if (renderer != null)
        {
            AddPiece(
                target
            );

            return;
        }

        for (int i = 0;
             i < target.childCount;
             i++)
        {
            CollectPiecesRecursive(
                target.GetChild(i)
            );
        }
    }


    private void AddPiece(
        Transform target)
    {
        if (target == null ||
            pieces.Contains(target))
        {
            return;
        }

        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers == null ||
            renderers.Length == 0)
        {
            return;
        }

        pieces.Add(
            target
        );
    }


    // =========================================================
    // PREPARE BREAK
    // =========================================================

    private void PreparePiecesForBreak()
    {
        // Disable the complete object's collision immediately. Colliders are
        // not always children of a rendered piece: many prefabs keep their
        // main collider on the root or a non-rendered container, and the
        // runtime blocker is also attached outside the piece list. Leaving
        // any of those active traps the tornado while visuals spiral inward.
        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider collider =
                colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] bodies =
            GetComponentsInChildren<Rigidbody>(
                true
            );

        for (int i = 0;
             i < bodies.Length;
             i++)
        {
            Rigidbody body =
                bodies[i];

            if (body == null)
            {
                continue;
            }

            body.linearVelocity =
                Vector3.zero;

            body.angularVelocity =
                Vector3.zero;

            body.useGravity =
                false;

            body.isKinematic =
                true;
        }
    }


    // =========================================================
    // CONSUME
    // =========================================================

    public bool CanConsumePiece(
        Tornado tornado,
        Transform target)
    {
        if (!activated ||
            tornado == null ||
            tornado.IsDefeated ||
            target == null)
        {
            return false;
        }

        if (!pieces.Contains(target))
        {
            return false;
        }

        if (consumedPieces.Contains(target))
        {
            return false;
        }

        return IsTornadoBigEnough(
            tornado
        );
    }


    public float GetPieceRequirement(
        Transform target)
    {
        return calculatedSizeRequirement;
    }


    /*
     * Exact Inspector Sugar Value.
     *
     * NO multiplier.
     * NO automatic reward.
     * NO weight calculation.
     */
    public float GetPieceSugarReward(
        Transform target)
    {
        if (sugarClaimed ||
            target == null ||
            !pieces.Contains(target))
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            sugarValue
        );
    }


    public float GetPiecePercentage(
        Transform target)
    {
        if (target == null ||
            !pieces.Contains(target))
        {
            return 0f;
        }

        return 1f;
    }


    public float ConsumePieceSugar(
        Transform target)
    {
        if (!activated ||
            target == null ||
            !pieces.Contains(target) ||
            consumedPieces.Contains(target))
        {
            return 0f;
        }

        consumedPieces.Add(
            target
        );

        /*
         * The entire destructible pays its
         * Inspector Sugar Value exactly ONCE.
         */
        if (sugarClaimed)
        {
            return 0f;
        }

        sugarClaimed = true;

        return Mathf.Max(
            0f,
            sugarValue
        );
    }


    // =========================================================
    // HIDE CONSUMED PIECE
    // =========================================================

    public void HideConsumedPiece(
        Transform target)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(
                true
            );

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer != null)
            {
                renderer.enabled =
                    false;
            }
        }

        Collider[] colliders =
            target.GetComponentsInChildren<Collider>(
                true
            );

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider collider =
                colliders[i];

            if (collider != null)
            {
                collider.enabled =
                    false;
            }
        }

        if (consumedPieces.Count >= pieces.Count)
        {
            enabled = false;
        }
    }


    // =========================================================
    // BREAKABLE PIECES
    // =========================================================

    public void GetBreakablePieces(
        List<Transform> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();

        if (!activated)
        {
            return;
        }

        for (int i = 0;
             i < pieces.Count;
             i++)
        {
            Transform piece =
                pieces[i];

            if (piece == null ||
                consumedPieces.Contains(piece))
            {
                continue;
            }

            result.Add(
                piece
            );
        }
    }


    // =========================================================
    // CANDY SPAWN BLOCKING
    // =========================================================

    public bool BlocksCandySpawn(
        Vector3 position,
        float radius)
    {
        if (!isActiveAndEnabled ||
            activated)
        {
            return false;
        }

        return BlocksPosition(
            position,
            radius
        );
    }


    // =========================================================
    // TORNADO / AI BLOCKING
    // =========================================================

    /*
     * Required by:
     *
     * TornadoAISpawner
     * TornadoAINavigation
     *
     * This checks the X/Z footprint of the object's renderers.
     */
    public bool BlocksTornadoPosition(
        Vector3 position,
        float radius)
    {
        if (!isActiveAndEnabled ||
            activated)
        {
            return false;
        }

        return BlocksPosition(
            position,
            radius
        );
    }


    // =========================================================
    // SHARED POSITION BLOCKING
    // =========================================================

    private bool BlocksPosition(
        Vector3 position,
        float radius)
    {
        Renderer[] renderers =
            GetCachedRenderers();

        if (renderers == null ||
            renderers.Length == 0)
        {
            return false;
        }

        float safeRadius =
            Mathf.Max(
                0f,
                radius
            );

        float radiusSquared =
            safeRadius *
            safeRadius;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];

            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds =
                renderer.bounds;

            float closestX =
                Mathf.Clamp(
                    position.x,
                    bounds.min.x,
                    bounds.max.x
                );

            float closestZ =
                Mathf.Clamp(
                    position.z,
                    bounds.min.z,
                    bounds.max.z
                );

            float dx =
                position.x -
                closestX;

            float dz =
                position.z -
                closestZ;

            float distanceSquared =
                dx * dx +
                dz * dz;

            if (distanceSquared <=
                radiusSquared)
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // RENDERERS
    // =========================================================

    public Renderer[] GetCachedRenderers()
    {
        if (cachedRenderers == null)
        {
            RefreshCache();
        }

        return cachedRenderers;
    }


    public void RefreshCache()
    {
        cachedRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );
    }


    // =========================================================
    // STATIC RESET
    // =========================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeDestructibles.Clear();
    }
}