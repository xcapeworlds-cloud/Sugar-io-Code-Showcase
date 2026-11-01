using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    // ==================================================
    // TARGET
    // ==================================================

    [Header("Target")]

    [SerializeField]
    private Tornado tornado;


    // ==================================================
    // CAMERA VIEW
    // ==================================================

    [Header("Camera View")]

    [Tooltip("Vertical field of view used by the gameplay camera.")]
    [SerializeField, Range(40f, 80f)]
    private float fieldOfView = 60f;

    [Tooltip(
        "How high the camera sits above the tornado. " +
        "Kept moderate to preserve the close 3D/isometric feel."
    )]
    [SerializeField, Min(1f)]
    private float cameraHeight = 12.5f;

    [Tooltip(
        "How far behind the tornado the camera sits. " +
        "Provides surrounding visibility without making the view top-down."
    )]
    [SerializeField, Min(0f)]
    private float cameraDistance = 12f;

    [Tooltip(
        "Vertical point on the tornado that the camera tracks. " +
        "0.5 aims at the middle of its visible height."
    )]
    [SerializeField, Range(0.25f, 0.65f)]
    private float verticalFraming = 0.46f;


    // ==================================================
    // FOLLOW
    // ==================================================

    [Header("Follow")]

    [Tooltip("How quickly the camera follows the tornado.")]
    [SerializeField, Min(0.01f)]
    private float followSmoothness = 7f;

    [Tooltip("How quickly the camera rotation settles.")]
    [SerializeField, Min(0.01f)]
    private float rotationSmoothness = 8f;


    // ==================================================
    // LOOK AHEAD
    // ==================================================

    [Header("Look Ahead")]

    [Tooltip(
        "Small framing shift toward movement direction. " +
        "The camera itself never rotates with player movement."
    )]
    [SerializeField, Range(0f, 5f)]
    private float lookAheadDistance = 0.8f;

    [SerializeField, Min(0.01f)]
    private float lookAheadSmoothness = 4f;

    [SerializeField, Min(0f)]
    private float lookAheadDeadZone = 0.02f;


    // ==================================================
    // GROWTH ZOOM
    // ==================================================

    [Header("Growth Zoom")]

    [SerializeField, Min(0.01f)]
    private float growthSmoothness = 2.5f;

    // ==================================================
    // RUNTIME
    // ==================================================

    private Camera gameplayCamera;

    private float smoothedZoom = 1f;

    private Vector3 smoothedLookAhead;

    private Vector3 previousTornadoPosition;

    private bool hasPreviousPosition;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        gameplayCamera = GetComponent<Camera>();

        if (gameplayCamera != null)
        {
            gameplayCamera.fieldOfView = fieldOfView;
        }

        FindPlayerIfNeeded();

        if (tornado == null)
        {
            Debug.LogWarning(
                "CameraFollow could not find the player Tornado.",
                this
            );

            enabled = false;
            return;
        }

        smoothedZoom = GetDesiredZoom();

        previousTornadoPosition =
            tornado.transform.position;

        hasPreviousPosition = true;

        smoothedLookAhead = Vector3.zero;

        Snap();
    }


    // ==================================================
    // LATE UPDATE
    // ==================================================

    private void LateUpdate()
    {
        if (tornado == null)
        {
            return;
        }

        Vector3 currentPosition =
            tornado.transform.position;


        // ==================================================
        // MOVEMENT
        // ==================================================

        Vector3 movement = Vector3.zero;

        if (hasPreviousPosition &&
            Time.deltaTime > 0f)
        {
            movement =
                currentPosition -
                previousTornadoPosition;
        }

        previousTornadoPosition =
            currentPosition;

        hasPreviousPosition = true;

        movement.y = 0f;


        // ==================================================
        // GROWTH ZOOM
        // ==================================================

        float zoomBlend =
            ExpBlend(
                growthSmoothness,
                Time.deltaTime
            );

        smoothedZoom =
            Mathf.Lerp(
                smoothedZoom,
                GetDesiredZoom(),
                zoomBlend
            );


        // ==================================================
        // LOOK AHEAD
        // ==================================================

        Vector3 desiredLookAhead =
            Vector3.zero;

        if (movement.sqrMagnitude >
            lookAheadDeadZone *
            lookAheadDeadZone)
        {
            desiredLookAhead =
                movement.normalized *
                lookAheadDistance;
        }

        float lookBlend =
            ExpBlend(
                lookAheadSmoothness,
                Time.deltaTime
            );

        smoothedLookAhead =
            Vector3.Lerp(
                smoothedLookAhead,
                desiredLookAhead,
                lookBlend
            );


        // ==================================================
        // CAMERA POSITION
        // ==================================================

        /*
         * Moderate height + increased backward distance.
         *
         * This gives the player more surrounding visibility
         * without solving the problem by raising the camera
         * into a top-down view.
         */

        Vector3 cameraOffset =
            new Vector3(
                0f,
                cameraHeight * smoothedZoom,
                -cameraDistance * smoothedZoom
            );

        /*
         * Only a small portion of look-ahead moves the camera.
         * This preserves useful playable space behind the tornado.
         */
        Vector3 cameraLookAhead =
            smoothedLookAhead * 0.25f;

        Vector3 wantedPosition =
            currentPosition +
            cameraOffset +
            cameraLookAhead;

        float followBlend =
            ExpBlend(
                followSmoothness,
                Time.deltaTime
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                wantedPosition,
                followBlend
            );


        // ==================================================
        // CAMERA LOOK TARGET
        // ==================================================

        /*
         * Keep the world orientation stable.
         *
         * Movement only makes a small framing adjustment.
         * It never changes the camera's orbit/yaw.
         */

        Vector3 lookTarget =
            currentPosition +
            Vector3.up * GetCurrentLookHeight() +
            smoothedLookAhead * 0.35f;

        Vector3 lookDirection =
            lookTarget -
            transform.position;

        if (lookDirection.sqrMagnitude >
            0.0001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up
                );

            float rotationBlend =
                ExpBlend(
                    rotationSmoothness,
                    Time.deltaTime
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationBlend
                );
        }
    }


    // ==================================================
    // ZOOM
    // ==================================================

    private float GetDesiredZoom()
    {
        if (tornado == null)
        {
            return 1f;
        }

        float scale =
            Mathf.Max(
                0.01f,
                tornado.VisualScale
            );

        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        float startingScale =
            match != null &&
            match.Balance != null
                ? match.Balance.startingVisualScale
                : 0.20f;

        if (match != null &&
            match.Balance != null)
        {
            return match.Balance.GetCameraZoom(
                scale
            );
        }

        // VisualScale is normally below 1 for much of a match. Using it
        // directly caused Mathf.Clamp to hold zoom at its minimum until the
        // tornado became enormous. Relative growth starts at 1 and responds
        // continuously as soon as the player grows.
        float relativeScale =
            scale /
            Mathf.Max(
                0.01f,
                startingScale
            );

        return Mathf.Pow(
            Mathf.Max(1f, relativeScale),
            0.65f
        );
    }

    private float GetCurrentLookHeight()
    {
        if (tornado == null)
        {
            return 0f;
        }

        return
            tornado.GetVisibleHeight() *
            verticalFraming;
    }


    // ==================================================
    // SNAP
    // ==================================================

    private void Snap()
    {
        if (tornado == null)
        {
            return;
        }

        Vector3 currentPosition =
            tornado.transform.position;

        Vector3 cameraOffset =
            new Vector3(
                0f,
                cameraHeight * smoothedZoom,
                -cameraDistance * smoothedZoom
            );

        transform.position =
            currentPosition +
            cameraOffset;

        Vector3 lookTarget =
            currentPosition +
            Vector3.up * GetCurrentLookHeight();

        Vector3 lookDirection =
            lookTarget -
            transform.position;

        if (lookDirection.sqrMagnitude >
            0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up
                );
        }
    }


    // ==================================================
    // FIND PLAYER
    // ==================================================

    private void FindPlayerIfNeeded()
    {
        if (tornado != null)
        {
            return;
        }

        PlayerController player =
            FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            tornado =
                player.GetComponent<Tornado>();
        }

        if (tornado != null)
        {
            return;
        }

        Tornado[] tornadoes =
            FindObjectsByType<Tornado>(
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < tornadoes.Length;
             i++)
        {
            Tornado candidate =
                tornadoes[i];

            if (candidate != null &&
                !candidate.IsAI &&
                !candidate.IsDefeated)
            {
                tornado = candidate;
                return;
            }
        }
    }


    // ==================================================
    // SMOOTHING
    // ==================================================

    private static float ExpBlend(
        float sharpness,
        float deltaTime)
    {
        return
            1f -
            Mathf.Exp(
                -sharpness *
                deltaTime
            );
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        fieldOfView =
            Mathf.Clamp(
                fieldOfView,
                40f,
                80f
            );

        cameraHeight =
            Mathf.Max(
                1f,
                cameraHeight
            );

        cameraDistance =
            Mathf.Max(
                0f,
                cameraDistance
            );

        verticalFraming =
            Mathf.Clamp(
                verticalFraming,
                0.25f,
                0.65f
            );

        followSmoothness =
            Mathf.Max(
                0.01f,
                followSmoothness
            );

        rotationSmoothness =
            Mathf.Max(
                0.01f,
                rotationSmoothness
            );

        lookAheadDistance =
            Mathf.Max(
                0f,
                lookAheadDistance
            );

        lookAheadSmoothness =
            Mathf.Max(
                0.01f,
                lookAheadSmoothness
            );

        lookAheadDeadZone =
            Mathf.Max(
                0f,
                lookAheadDeadZone
            );

        growthSmoothness =
            Mathf.Max(
                0.01f,
                growthSmoothness
            );

    }
}