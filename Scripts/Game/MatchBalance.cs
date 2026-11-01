using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public sealed class MatchBalance
{
    // ==================================================
    // TORNADO GROWTH
    // ==================================================

    [Header("Tornado Growth")]

    [Tooltip("Visual size of a tornado at zero sugar.")]
    [Range(0.05f, 1f)]
    public float startingVisualScale = 0.20f;

    [Tooltip(
        "How much physical growth each stored sugar produces. " +
        "1 = normal, 0.5 = half growth, 2 = double growth. " +
        "This never changes the actual sugar reward."
    )]
    [Range(0f, 3f)]
    public float sugarGrowthStrength = 1f;

    // Kept serialized so existing scenes retain their established growth
    // curve. This is an implementation coefficient, not a balance control.
    [FormerlySerializedAs("growthStrength")]
    [SerializeField, HideInInspector]
    private float baseGrowthPerSqrtSugar = 0.047f;

    public float GrowthPerSqrtSugar =>
        baseGrowthPerSqrtSugar *
        sugarGrowthStrength;

    [Tooltip(
        "How strongly the camera follows tornado growth. " +
        "0 keeps the original distance and 1 matches size exactly."
    )]
    [Range(0f, 1f)]
    public float cameraGrowthResponse = 0.65f;

    [Tooltip("How quickly the visible tornado catches up to earned growth.")]
    [Range(1f, 20f)]
    public float visualGrowthSpeed = 7f;

    [Tooltip("Rotation speed of the cotton-candy swirl in degrees per second.")]
    [Min(0f)]
    public float tornadoSpinSpeed = 270f;

    [Tooltip("Maximum spin-speed multiplier reached by a very large tornado.")]
    [Min(1f)]
    public float maximumSpinMultiplier = 1.75f;

    [Tooltip("How quickly spin speed approaches its maximum as the tornado grows.")]
    [Min(0f)]
    public float spinGrowthResponse = 0.35f;

    public float GetVisualScale(float sugar)
    {
        return
            startingVisualScale +
            Mathf.Sqrt(
                Mathf.Max(0f, sugar)
            ) * GrowthPerSqrtSugar;
    }

    public float GetCameraZoom(float visualScale)
    {
        float relativeScale =
            Mathf.Max(0.01f, visualScale) /
            Mathf.Max(0.01f, startingVisualScale);

        return Mathf.Pow(
            Mathf.Max(1f, relativeScale),
            cameraGrowthResponse
        );
    }

    [Header("Object Consumption")]

    [Tooltip(
        "How much of an object's horizontal radius the tornado must cover. " +
        "0.6 means radius checks are 40% more forgiving. " +
        "Height requirements are not changed."
    )]
    [Range(0.5f, 1f)]
    public float radiusRequirementMultiplier = 0.6f;


    // ==================================================
    // MOVEMENT
    // ==================================================

    [Header("Movement")]

    [Tooltip("Base movement speed before size slowdown.")]
    [Range(3f, 25f)]
    public float baseMoveSpeed = 13f;

    [Tooltip(
        "Lowest movement-speed multiplier a large tornado can reach. " +
        "Lower values make it easier for small tornadoes to escape."
    )]
    [Range(0.25f, 1f)]
    public float minimumSpeedMultiplier = 0.65f;


    // ==================================================
    // CANDY PICKUP
    // ==================================================

    [Header("Candy Pickup")]

    [Tooltip("Pickup range near the beginning of a match.")]
    [Range(0.5f, 8f)]
    public float baseCandyPickupRange = 6f;

    [Tooltip("Maximum candy pickup range after growing.")]
    [Range(1f, 12f)]
    public float maximumCandyPickupRange = 10f;


    // ==================================================
    // TORNADO COMBAT
    // ==================================================

    [Header("Tornado Combat")]

    [Tooltip(
        "Required visible-size ratio early in the match. " +
        "1.12 means the attacker must look about 12% larger."
    )]
    [Range(1.01f, 2f)]
    public float earlyConsumeRatio = 1.12f;

    [Tooltip(
        "Required visible-size ratio later in progression."
    )]
    [Range(1.01f, 2f)]
    public float lateConsumeRatio = 1.06f;

    [Tooltip(
        "How much tornado overlap is required before capture."
    )]
    [Range(0f, 0.75f)]
    public float combatRequiredOverlap = 0.25f;

    [Tooltip(
        "Extra reach around the visible funnel used to begin a capture. " +
        "This avoids requiring both floor pivots to nearly overlap."
    )]
    [Range(0f, 3f)]
    public float combatCapturePadding = 3.15f;

    [Tooltip(
        "Extra reaction time given to the human player " +
        "before an AI tornado captures them."
    )]
    [Range(0f, 2f)]
    public float playerCaptureGraceTime = 0.40f;

    [Tooltip("Duration of the tornado-eating animation.")]
    [Range(0.05f, 1.5f)]
    public float tornadoConsumeDuration = 0.24f;

    [Tooltip(
        "Fraction of a defeated tornado's sugar transferred to its killer. " +
        "0.30 means a 100-sugar victim awards 30 sugar."
    )]
    [Range(0f, 1f)]
    public float tornadoSugarTransferMultiplier = 0.30f;


    // ==================================================
    // ENVIRONMENT
    // ==================================================

    [Header("Environment Interaction")]

    [Tooltip(
        "How close the tornado must be before it can break " +
        "an environment object."
    )]
    [Range(1f, 15f)]
    public float environmentBreakRadius = 10f;

    [Tooltip(
        "Distance at which almost-breakable objects begin warning."
    )]
    [Range(1f, 20f)]
    public float environmentWarningRadius = 13f;


    // ==================================================
    // ENVIRONMENT WARNING
    // ==================================================

    [Header("Environment Warning Wobble")]

    [Tooltip(
        "Object begins wobbling when the tornado reaches " +
        "this fraction of the required size."
    )]
    [Range(0.25f, 0.99f)]
    public float wiggleStartRatio = 0.72f;

    [Tooltip("Maximum warning rotation in degrees.")]
    [Range(0f, 12f)]
    public float wiggleMaximumAngle = 5f;

    [Tooltip("Maximum warning positional movement.")]
    [Range(0f, 0.30f)]
    public float wiggleMaximumOffset = 0.10f;

    [Tooltip("Speed of the warning wobble.")]
    [Range(1f, 25f)]
    public float wiggleSpeed = 10.5f;


    // ==================================================
    // ENVIRONMENT SUCTION
    // ==================================================

    [Header("Environment Suction")]

    [Tooltip("Time spent doing the initial crumble movement.")]
    [Min(0.01f)]
    public float crumbleDuration = 0.65f;

    [Tooltip("How far pieces initially crumble outward.")]
    [Range(0f, 2f)]
    public float crumbleDistance = 0.58f;

    [Tooltip("How far pieces initially drop.")]
    [Range(0f, 1f)]
    public float crumbleDrop = 0.55f;

    [Tooltip("Time for a broken piece to spiral into the tornado.")]
    [Min(0.01f)]
    public float suctionDuration = 0.82f;

    [Tooltip("Number of spiral rotations during suction.")]
    [Range(0.1f, 5f)]
    public float spiralTurns = 1.45f;

    [Tooltip("Piece rotation speed while being sucked in.")]
    [Range(0f, 1000f)]
    public float suctionSpinSpeed = 440f;

    [Tooltip(
        "Final reward multiplier for destructible props. " +
        "Legacy scene objects may contain values near 100, so 0.10 turns " +
        "those into a balanced 10 sugar without changing visual size."
    )]
    [Range(0f, 1f)]
    public float environmentSugarMultiplier = 0.10f;


    // ==================================================
    // AI TARGETING
    // ==================================================

    [Header("AI Targeting")]

    [Tooltip(
        "How far AI looks for candy."
    )]
    [Range(5f, 50f)]
    public float aiCandyRange = 26f;

    [Tooltip(
        "Normal maximum distance AI considers tornado prey."
    )]
    [Range(5f, 50f)]
    public float aiHuntRange = 25f;

    [Tooltip(
        "Maximum range from which AI deliberately targets the player."
    )]
    [Range(5f, 50f)]
    public float aiPlayerHuntRange = 24f;

    [Tooltip(
        "Seconds after AI spawns before it can deliberately hunt the player."
    )]
    [Range(0f, 20f)]
    public float aiPlayerSafeTime = 5f;


    // ==================================================
    // AI MOVEMENT
    // ==================================================

    [Header("AI Movement")]

    [Tooltip(
        "Movement throttle while wandering."
    )]
    [Range(0.1f, 1f)]
    public float aiWanderThrottle = 0.72f;

    [Tooltip(
        "Movement throttle while actively hunting."
    )]
    [Range(0.1f, 1f)]
    public float aiHuntThrottle = 1f;


    // ==================================================
    // AI HUNTING
    // ==================================================

    [Header("AI Hunting")]

    [Tooltip(
        "Sugar below this amount is considered early game."
    )]
    [Range(0f, 100f)]
    public float aiEarlyGameSugar = 8f;

    [Tooltip(
        "Sugar level where AI becomes properly aggressive."
    )]
    [Range(0f, 150f)]
    public float aiAggressiveSugar = 20f;

    // ==================================================
    // AI CANDY PERSONALITIES
    // ==================================================

    [Header("AI Candy Personalities")]

    [Tooltip(
        "Candy pickup-range multiplier used by Collector AI."
    )]
    [Range(0.1f, 1.5f)]
    public float aiCollectorVacuumMultiplier = 0.90f;

    [Tooltip(
        "Candy pickup-range multiplier used by Hunter AI."
    )]
    [Range(0.1f, 1.5f)]
    public float aiHunterVacuumMultiplier = 0.72f;


    // ==================================================
    // VALIDATION
    // ==================================================

    public void Validate()
    {
        // ----------------------------------------------
        // TORNADO
        // ----------------------------------------------

        startingVisualScale =
            Mathf.Max(
                0.01f,
                startingVisualScale
            );

        sugarGrowthStrength =
            Mathf.Max(
                0f,
                sugarGrowthStrength
            );

        baseGrowthPerSqrtSugar =
            Mathf.Max(
                0.001f,
                baseGrowthPerSqrtSugar
            );

        cameraGrowthResponse =
            Mathf.Clamp01(
                cameraGrowthResponse
            );

        visualGrowthSpeed =
            Mathf.Max(
                0.01f,
                visualGrowthSpeed
            );

        tornadoSpinSpeed =
            Mathf.Max(
                0f,
                tornadoSpinSpeed
            );

        maximumSpinMultiplier =
            Mathf.Max(
                1f,
                maximumSpinMultiplier
            );

        spinGrowthResponse =
            Mathf.Max(
                0f,
                spinGrowthResponse
            );

        radiusRequirementMultiplier =
            Mathf.Clamp(
                radiusRequirementMultiplier,
                0.5f,
                1f
            );

        baseMoveSpeed =
            Mathf.Max(
                0f,
                baseMoveSpeed
            );

        minimumSpeedMultiplier =
            Mathf.Clamp01(
                minimumSpeedMultiplier
            );


        // ----------------------------------------------
        // CANDY PICKUP
        // ----------------------------------------------

        baseCandyPickupRange =
            Mathf.Max(
                0f,
                baseCandyPickupRange
            );

        maximumCandyPickupRange =
            Mathf.Max(
                baseCandyPickupRange,
                maximumCandyPickupRange
            );


        // ----------------------------------------------
        // COMBAT
        // ----------------------------------------------

        earlyConsumeRatio =
            Mathf.Max(
                1.01f,
                earlyConsumeRatio
            );

        lateConsumeRatio =
            Mathf.Max(
                1.01f,
                lateConsumeRatio
            );

        combatRequiredOverlap =
            Mathf.Clamp(
                combatRequiredOverlap,
                0f,
                0.75f
            );

        combatCapturePadding =
            Mathf.Max(
                0f,
                combatCapturePadding
            );

        playerCaptureGraceTime =
            Mathf.Max(
                0f,
                playerCaptureGraceTime
            );

        tornadoConsumeDuration =
            Mathf.Max(
                0.01f,
                tornadoConsumeDuration
            );

        tornadoSugarTransferMultiplier =
            Mathf.Clamp01(
                tornadoSugarTransferMultiplier
            );


        // ----------------------------------------------
        // ENVIRONMENT
        // ----------------------------------------------

        environmentBreakRadius =
            Mathf.Max(
                0.1f,
                environmentBreakRadius
            );

        environmentWarningRadius =
            Mathf.Max(
                environmentBreakRadius,
                environmentWarningRadius
            );

        wiggleStartRatio =
            Mathf.Clamp(
                wiggleStartRatio,
                0.01f,
                0.99f
            );

        wiggleMaximumAngle =
            Mathf.Max(
                0f,
                wiggleMaximumAngle
            );

        wiggleMaximumOffset =
            Mathf.Max(
                0f,
                wiggleMaximumOffset
            );

        wiggleSpeed =
            Mathf.Max(
                0f,
                wiggleSpeed
            );

        crumbleDuration =
            Mathf.Max(
                0.01f,
                crumbleDuration
            );

        crumbleDistance =
            Mathf.Max(
                0f,
                crumbleDistance
            );

        crumbleDrop =
            Mathf.Max(
                0f,
                crumbleDrop
            );

        suctionDuration =
            Mathf.Max(
                0.01f,
                suctionDuration
            );

        spiralTurns =
            Mathf.Max(
                0f,
                spiralTurns
            );

        suctionSpinSpeed =
            Mathf.Max(
                0f,
                suctionSpinSpeed
            );

        environmentSugarMultiplier =
            Mathf.Max(
                0f,
                environmentSugarMultiplier
            );


        // ----------------------------------------------
        // AI TARGETING
        // ----------------------------------------------

        aiCandyRange =
            Mathf.Max(
                0f,
                aiCandyRange
            );

        aiHuntRange =
            Mathf.Max(
                0f,
                aiHuntRange
            );

        aiPlayerHuntRange =
            Mathf.Max(
                0f,
                aiPlayerHuntRange
            );

        aiPlayerSafeTime =
            Mathf.Max(
                0f,
                aiPlayerSafeTime
            );


        // ----------------------------------------------
        // AI MOVEMENT
        // ----------------------------------------------

        aiWanderThrottle =
            Mathf.Clamp01(
                aiWanderThrottle
            );

        aiHuntThrottle =
            Mathf.Clamp01(
                aiHuntThrottle
            );


        // ----------------------------------------------
        // AI HUNTING
        // ----------------------------------------------

        aiEarlyGameSugar =
            Mathf.Max(
                0f,
                aiEarlyGameSugar
            );

        aiAggressiveSugar =
            Mathf.Max(
                aiEarlyGameSugar,
                aiAggressiveSugar
            );

        // ----------------------------------------------
        // AI CANDY
        // ----------------------------------------------

        aiCollectorVacuumMultiplier =
            Mathf.Max(
                0.01f,
                aiCollectorVacuumMultiplier
            );

        aiHunterVacuumMultiplier =
            Mathf.Max(
                0.01f,
                aiHunterVacuumMultiplier
            );
    }
}