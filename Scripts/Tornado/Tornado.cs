using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TornadoColors))]
public sealed class Tornado : MonoBehaviour
{
    private static readonly List<Tornado> activeTornadoes = new();

    private const float StartingSugar = 0f;

    // ==================================================
    // FALLBACK BALANCE
    // ==================================================

    private const float FallbackStartingVisualScale = 0.20f;
    private const float FallbackGrowthStrength = 0.047f;

    private const float FallbackBaseMoveSpeed = 13f;
    private const float FallbackMinimumSpeedMultiplier = 0.65f;

    private const float FallbackBaseCandyPickupRange = 3f;
    private const float FallbackMaximumCandyPickupRange = 5f;

    private const float FallbackEarlyConsumeRatio = 1.12f;
    private const float FallbackLateConsumeRatio = 1.06f;

    // ==================================================
    // GROWTH
    // ==================================================

    private const float BaseTornadoHeight = 8.08f;
    private const float BaseTornadoRadius = 1.68f;

    private const float UtilityProgressSugar = 500f;
    private const float SizeSpeedPenaltyCurve = 0.8f;

    // ==================================================
    // CANDY
    // ==================================================

    private const float CandyScanInterval = 0.08f;

    // ==================================================
    // COMBAT INTERNALS
    // ==================================================

    private const float FullCombatStrengthSugar = 100f;

    // ==================================================
    // ENVIRONMENT COLLISION
    // ==================================================

    private const float CollisionRadiusAtScaleOne = 1.05f;
    private const float MinimumCollisionRadius = 0.22f;

    // ==================================================
    // RUNTIME
    // ==================================================

    private Rigidbody body;
    private TornadoCombat combat;
    private TornadoAI aiController;

    private Vector3 moveIntent;

    private float currentSugar;
    private float movementThrottle = 1f;
    private float nextCandyScanTime;

    private bool isDefeated;
    private bool isAI;

    private readonly Collider[] environmentHits =
        new Collider[32];

    private readonly List<Candy> nearbyCandy =
        new List<Candy>(64);

    // ==================================================
    // PUBLIC
    // ==================================================

    public static IReadOnlyList<Tornado> ActiveTornadoes =>
        activeTornadoes;

    public float Sugar =>
        currentSugar;

    public bool IsDefeated =>
        isDefeated;

    public bool IsAI =>
        isAI;

    public bool IsAtZeroSugar =>
        currentSugar <= 0f;

    public bool IsActivelyMoving =>
        movementThrottle > 0.08f &&
        moveIntent.sqrMagnitude > 0.01f;

    public bool IsFinishingConsumption =>
        combat != null &&
        combat.IsCaptured;

    public TornadoColors Colors
    {
        get;
        private set;
    }

    public Vector3 BottomTipWorldPosition =>
        transform.position;

    public event System.Action<float> SugarChanged;
    public event System.Action<float> GrowthChanged;

    // ==================================================
    // BALANCE ACCESS
    // ==================================================

    private MatchBalance GetBalance()
    {
        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        return
            match != null
                ? match.Balance
                : null;
    }

    // ==================================================
    // VISUAL SCALE
    // ==================================================

    public float VisualScale
    {
        get
        {
            MatchBalance balance =
                GetBalance();

            float startingScale =
                balance != null
                    ? balance.startingVisualScale
                    : FallbackStartingVisualScale;

            float growthStrength =
                balance != null
                    ? balance.GrowthPerSqrtSugar
                    : FallbackGrowthStrength;

            if (balance != null)
            {
                return balance.GetVisualScale(
                    currentSugar
                );
            }

            return
                startingScale +
                Mathf.Sqrt(
                    Mathf.Max(0f, currentSugar)
                ) * growthStrength;
        }
    }

    public float GetVisibleHeight()
    {
        return
            BaseTornadoHeight *
            VisualScale;
    }

    public float GetVisibleRadius()
    {
        return
            BaseTornadoRadius *
            VisualScale;
    }

    public bool CanFitObject(
        float objectHeight,
        float objectRadius)
    {
        MatchBalance balance =
            GetBalance();

        float radiusMultiplier =
            balance != null
                ? balance.radiusRequirementMultiplier
                : 0.6f;

        return
            GetVisibleHeight() >= objectHeight &&
            GetVisibleRadius() >=
            objectRadius * radiusMultiplier;
    }

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        body =
            GetComponent<Rigidbody>();

        combat =
            GetComponent<TornadoCombat>();

        Colors =
            GetComponent<TornadoColors>();

        aiController =
            GetComponent<TornadoAI>();

        isAI =
            aiController != null;

        currentSugar =
            StartingSugar;

        body.isKinematic =
            true;

        body.useGravity =
            false;

        body.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        if (!activeTornadoes.Contains(this))
        {
            activeTornadoes.Add(this);
        }

        // Spread candy scans across frames so every tornado does not walk
        // the full active-candy list during the same physics tick.
        nextCandyScanTime =
            Time.time +
            Mathf.Abs(GetInstanceID() % 1000) /
            1000f * CandyScanInterval;
    }

    private void OnDisable()
    {
        activeTornadoes.Remove(this);
    }

    private void FixedUpdate()
    {
        if (isDefeated ||
            IsFinishingConsumption)
        {
            return;
        }

        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (match != null &&
            !match.IsPlaying)
        {
            return;
        }

        Move();

        if (Time.time >= nextCandyScanTime)
        {
            nextCandyScanTime =
                Time.time +
                CandyScanInterval;

            EatNearbyCandy();
        }
    }

    // ==================================================
    // MOVEMENT
    // ==================================================

    private void Move()
    {
        if (body == null)
        {
            return;
        }

        Vector3 direction =
            moveIntent;

        direction.y =
            0f;

        direction =
            Vector3.ClampMagnitude(
                direction,
                1f
            );

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        float speed =
            GetMoveSpeed();

        Vector3 movement =
            direction *
            speed *
            movementThrottle *
            Time.fixedDeltaTime;

        Vector3 start =
            body.position;

        Vector3 destination =
            start +
            movement;

        destination.y =
            start.y;

        if (PlayingField.Instance != null)
        {
            destination =
                PlayingField.Instance.ClampPosition(
                    destination,
                    GetEnvironmentCollisionRadius()
                );

            destination.y =
                start.y;
        }

        destination =
            ResolveEnvironmentCollision(
                start,
                destination
            );

        body.MovePosition(
            destination
        );
    }

    // ==================================================
    // ENVIRONMENT COLLISION
    // ==================================================

    private Vector3 ResolveEnvironmentCollision(
        Vector3 start,
        Vector3 destination)
    {
        float radius =
            GetEnvironmentCollisionRadius();

        if (IsEnvironmentPositionClear(
                destination,
                radius))
        {
            return destination;
        }

        Vector3 movement =
            destination - start;

        movement.y = 0f;

        float distance =
            movement.magnitude;

        if (distance < 0.0001f)
        {
            return start;
        }

        Vector3 forward =
            movement / distance;

        // Try shallow turns first so movement naturally slides along an
        // object's edge. Wider turns provide an escape when the tornado is
        // pressed into a corner instead of freezing at its current position.
        for (int distancePass = 0;
             distancePass < 2;
             distancePass++)
        {
            float stepDistance =
                distancePass == 0
                    ? distance
                    : distance * 0.5f;

            for (int angleStep = 1;
                 angleStep <= 5;
                 angleStep++)
            {
                float angle =
                    angleStep * 30f;

                for (int side = -1;
                     side <= 1;
                     side += 2)
                {
                    Vector3 direction =
                        Quaternion.Euler(
                            0f,
                            angle * side,
                            0f
                        ) * forward;

                    Vector3 candidate =
                        start +
                        direction * stepDistance;

                    candidate.y = start.y;

                    if (IsEnvironmentPositionClear(
                            candidate,
                            radius))
                    {
                        return candidate;
                    }
                }
            }
        }

        return start;
    }

    private bool IsEnvironmentPositionClear(
        Vector3 position,
        float radius)
    {
        // Use the destructible system's cached X/Z footprint first. This is
        // deterministic and still works when a prefab collider is missing,
        // disabled, offset, or too thin for the movement step to detect.
        IReadOnlyList<TornadoDestructible> destructibles =
            TornadoDestructible.ActiveDestructibles;

        for (int i = 0; i < destructibles.Count; i++)
        {
            TornadoDestructible destructible = destructibles[i];

            if (destructible == null ||
                !destructible.isActiveAndEnabled ||
                destructible.IsActivated ||
                destructible.IsWaitingForRespawn ||
                destructible.IsTornadoBigEnough(this))
            {
                continue;
            }

            if (destructible.BlocksTornadoPosition(position, radius))
            {
                return false;
            }
        }

        Vector3 center =
            position +
            Vector3.up *
            Mathf.Max(
                radius,
                0.25f
            );

        int hitCount =
            Physics.OverlapSphereNonAlloc(
                center,
                radius,
                environmentHits,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider hit =
                environmentHits[i];

            if (hit == null ||
                !hit.enabled ||
                hit.isTrigger)
            {
                continue;
            }

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            TornadoDestructible destructible =
                hit.GetComponentInParent<TornadoDestructible>();

            if (destructible == null)
            {
                continue;
            }

            if (destructible.IsActivated ||
                destructible.IsWaitingForRespawn)
            {
                continue;
            }

            if (destructible.IsTornadoBigEnough(this))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private float GetEnvironmentCollisionRadius()
    {
        return
            Mathf.Max(
                MinimumCollisionRadius,
                CollisionRadiusAtScaleOne *
                VisualScale
            );
    }

    // ==================================================
    // SPEED
    // ==================================================

    public float GetMoveSpeed()
    {
        MatchBalance balance =
            GetBalance();

        float baseMoveSpeed =
            balance != null
                ? balance.baseMoveSpeed
                : FallbackBaseMoveSpeed;

        float minimumSpeedMultiplier =
            balance != null
                ? balance.minimumSpeedMultiplier
                : FallbackMinimumSpeedMultiplier;

        float startingScale =
            balance != null
                ? balance.startingVisualScale
                : FallbackStartingVisualScale;

        float relativeGrowth =
            Mathf.Max(
                0f,
                VisualScale /
                Mathf.Max(0.01f, startingScale) -
                1f
            );

        // Speed now responds to visible size instead of waiting until a
        // tornado approaches 500 sugar. This gives smaller tornadoes a fair,
        // immediately noticeable escape advantage under the same rules.
        float progress =
            1f -
            Mathf.Exp(
                -relativeGrowth *
                SizeSpeedPenaltyCurve
            );

        float multiplier =
            Mathf.Lerp(
                1f,
                minimumSpeedMultiplier,
                progress
            );

        return
            baseMoveSpeed *
            multiplier;
    }

    // ==================================================
    // CANDY
    // ==================================================

    private void EatNearbyCandy()
    {
        MatchBalance balance =
            GetBalance();

        float basePickupRange =
            balance != null
                ? balance.baseCandyPickupRange
                : FallbackBaseCandyPickupRange;

        float maximumPickupRange =
            balance != null
                ? balance.maximumCandyPickupRange
                : FallbackMaximumCandyPickupRange;

        float progress =
            Mathf.Clamp01(
                currentSugar /
                UtilityProgressSugar
            );

        float range =
            Mathf.Lerp(
                basePickupRange,
                maximumPickupRange,
                progress
            );

        if (isAI &&
            aiController != null)
        {
            range *=
                aiController.CandyVacuumMultiplier;
        }

        float rangeSquared =
            range *
            range;

        Vector3 tornadoPosition =
            transform.position;

        Candy.GetNearbyNonAlloc(
            tornadoPosition,
            range,
            nearbyCandy
        );

        for (int i = nearbyCandy.Count - 1;
             i >= 0;
             i--)
        {
            Candy candy =
                nearbyCandy[i];

            if (candy == null ||
                candy.IsBeingAbsorbed ||
                !candy.CanBeEatenBy(this))
            {
                continue;
            }

            Vector3 difference =
                candy.transform.position -
                tornadoPosition;

            difference.y =
                0f;

            if (difference.sqrMagnitude >
                rangeSquared)
            {
                continue;
            }

            candy.Eat(this);
        }
    }

    // ==================================================
    // MOVEMENT INPUT
    // ==================================================

    public void SetMoveIntent(
        Vector3 direction,
        float throttle = 1f)
    {
        if (isDefeated ||
            IsFinishingConsumption)
        {
            return;
        }

        direction.y =
            0f;

        moveIntent =
            Vector3.ClampMagnitude(
                direction,
                1f
            );

        movementThrottle =
            Mathf.Clamp01(
                throttle
            );
    }

    public void SetCombatPosition(
        Vector3 position)
    {
        if (isDefeated ||
            body == null)
        {
            return;
        }

        body.MovePosition(
            position
        );
    }

    // ==================================================
    // COMBAT
    // ==================================================

    public bool CanConsume(
        Tornado other)
    {
        if (other == null ||
            other == this ||
            isDefeated ||
            other.isDefeated)
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

        MatchBalance balance =
            GetBalance();

        float earlyConsumeRatio =
            balance != null
                ? balance.earlyConsumeRatio
                : FallbackEarlyConsumeRatio;

        float lateConsumeRatio =
            balance != null
                ? balance.lateConsumeRatio
                : FallbackLateConsumeRatio;

        float combatProgress =
            Mathf.Clamp01(
                currentSugar /
                FullCombatStrengthSugar
            );

        float consumeRatio =
            Mathf.Lerp(
                earlyConsumeRatio,
                lateConsumeRatio,
                combatProgress
            );

        return
            VisualScale >=
            other.VisualScale *
            consumeRatio;
    }

    // ==================================================
    // SUGAR
    // ==================================================

    public bool AddSugar(
        float amount,
        Color color)
    {
        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (isDefeated ||
            amount <= 0f ||
            (match != null &&
             !match.IsPlaying))
        {
            return false;
        }

        float oldSugar =
            currentSugar;

        // IMPORTANT:
        // Sugar is stored EXACTLY as awarded.
        // 1 sugar = +1.
        // 2 sugar = +2.
        // Sugar Growth Strength does NOT touch this value.
        currentSugar =
            Mathf.Max(
                0f,
                currentSugar +
                amount
            );

        float gained =
            currentSugar -
            oldSugar;

        if (gained <= 0f)
        {
            return false;
        }

        if (Colors != null)
        {
            Colors.AddColor(
                color,
                gained
            );
        }

        if (!isAI)
        {
            GameAudioManager.Instance
                ?.ReportPlayerGrowth(
                    VisualScale
                );
        }

        SugarChanged?.Invoke(
            currentSugar
        );

        GrowthChanged?.Invoke(
            VisualScale
        );

        return true;
    }

    // ==================================================
    // DEFEAT
    // ==================================================

    public void Defeat(
        Tornado killer = null,
        float transferredSugar = 0f)
    {
        if (isDefeated)
        {
            return;
        }

        TornadoMatchManager.Instance?.ReportElimination(
            killer,
            this,
            Mathf.Max(
                0f,
                transferredSugar
            )
        );

        isDefeated =
            true;

        currentSugar =
            0f;

        moveIntent =
            Vector3.zero;

        movementThrottle =
            0f;

        activeTornadoes.Remove(
            this
        );

        Destroy(
            gameObject
        );
    }

    // ==================================================
    // STATIC RESET
    // ==================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeTornadoes.Clear();
    }
}