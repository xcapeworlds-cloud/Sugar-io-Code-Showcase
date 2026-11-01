using UnityEngine;

/// <summary>
/// Provides the authoritative playable XZ bounds
/// for the currently active level.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class PlayingField : MonoBehaviour
{
    public static PlayingField Instance { get; private set; }

    public Bounds PlayableBounds => GetPaddedBounds();

    [SerializeField, Min(0f)]
    private float edgePadding = 2f;

    private Renderer floorRenderer;

    private void Awake()
    {
        floorRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (floorRenderer == null)
            floorRenderer = GetComponent<Renderer>();

        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "More than one PlayingField is active. " +
                "Only one level should be enabled at a time.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public Vector3 GetRandomPoint()
    {
        Bounds bounds = GetPaddedBounds();

        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            transform.position.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        return ClampPosition(
            position,
            0f
        );
    }

    public Vector3 ClampPosition(
        Vector3 position,
        float clearance)
    {
        Bounds bounds =
            GetBoundsWithClearance(
                clearance
            );

        position.x = Mathf.Clamp(
            position.x,
            bounds.min.x,
            bounds.max.x
        );

        position.z = Mathf.Clamp(
            position.z,
            bounds.min.z,
            bounds.max.z
        );

        return position;
    }

    public bool IsInside(Vector3 position)
    {
        return IsInside(
            position,
            0f
        );
    }

    public bool IsInside(
        Vector3 position,
        float clearance)
    {
        Bounds bounds =
            GetBoundsWithClearance(
                clearance
            );

        return
            position.x >= bounds.min.x &&
            position.x <= bounds.max.x &&
            position.z >= bounds.min.z &&
            position.z <= bounds.max.z;
    }

    private Bounds GetBoundsWithClearance(
        float clearance)
    {
        Bounds bounds =
            GetPaddedBounds();

        float safeClearance =
            Mathf.Min(
                Mathf.Max(0f, clearance),
                Mathf.Min(
                    bounds.extents.x,
                    bounds.extents.z
                ) * 0.9f
            );

        bounds.Expand(
            new Vector3(
                -safeClearance * 2f,
                0f,
                -safeClearance * 2f
            )
        );

        return bounds;
    }

    private Bounds GetPaddedBounds()
    {
        if (floorRenderer == null)
            floorRenderer = GetComponent<Renderer>();

        Bounds bounds = floorRenderer.bounds;

        float safePadding = Mathf.Min(
            edgePadding,
            Mathf.Min(
                bounds.extents.x,
                bounds.extents.z
            ) * 0.9f
        );

        bounds.Expand(
            new Vector3(
                -safePadding * 2f,
                0f,
                -safePadding * 2f
            )
        );

        return bounds;
    }
}