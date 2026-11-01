using UnityEngine;

public sealed class TornadoAINavigation
{
    private readonly Tornado tornado;
    private readonly TornadoCombat combat;

    private Vector3 wanderDirection;

    private Vector3 lastStuckPosition;

    private float nextWanderTime;
    private float nextStuckCheckTime;

    private int failedMovementChecks;

    private float recoveryUntil;
    private Vector3 recoveryDirection;


    // ==================================================
    // CONSTRUCTION
    // ==================================================

    public TornadoAINavigation(
        Tornado tornado,
        TornadoCombat combat)
    {
        this.tornado =
            tornado;

        this.combat =
            combat;

        lastStuckPosition =
            tornado != null
                ? tornado.transform.position
                : Vector3.zero;

        nextStuckCheckTime =
            Time.time +
            TornadoAIBalance.StuckCheckInterval;

        PickWanderDirection();
    }


    // ==================================================
    // PUBLIC STATE
    // ==================================================

    public bool IsRecovering =>
        Time.time <
        recoveryUntil;


    public Vector3 RecoveryDirection =>
        recoveryDirection;


    public Vector3 WanderDirection =>
        wanderDirection;


    // ==================================================
    // RESET
    // ==================================================

    public void ResetRuntime()
    {
        failedMovementChecks =
            0;

        recoveryUntil =
            0f;

        recoveryDirection =
            Vector3.zero;

        nextWanderTime =
            0f;


        if (tornado != null)
        {
            lastStuckPosition =
                tornado.transform.position;
        }


        nextStuckCheckTime =
            Time.time +
            TornadoAIBalance.StuckCheckInterval;


        PickWanderDirection();
    }


    public void ResetStuckDetection()
    {
        if (tornado == null)
        {
            return;
        }


        lastStuckPosition =
            tornado.transform.position;


        nextStuckCheckTime =
            Time.time +
            TornadoAIBalance.StuckCheckInterval;


        failedMovementChecks =
            0;


        recoveryUntil =
            0f;
    }


    // ==================================================
    // WANDER
    // ==================================================

    public Vector3 GetWanderDirection()
    {
        if (Time.time >=
                nextWanderTime ||
            wanderDirection.sqrMagnitude <
                0.01f)
        {
            PickWanderDirection();
        }


        return
            wanderDirection;
    }


    public void PickWanderDirection()
    {
        Vector2 random =
            Random.insideUnitCircle;


        if (random.sqrMagnitude <
            0.05f)
        {
            random =
                Vector2.right;
        }


        random.Normalize();


        Vector3 randomDirection =
            new Vector3(
                random.x,
                0f,
                random.y
            );


        Vector3 centerDirection =
            GetDirectionTowardFieldCenter();


        wanderDirection =
            FlatDirection(
                randomDirection +
                centerDirection *
                0.35f
            );


        if (wanderDirection.sqrMagnitude <
            0.01f)
        {
            wanderDirection =
                randomDirection;
        }


        nextWanderTime =
            Time.time +
            TornadoAIBalance.WanderChangeTime;
    }


    // ==================================================
    // FINAL MOVEMENT DIRECTION
    // ==================================================

    public Vector3 BuildMovementDirection(
        Vector3 desiredDirection)
    {
        Vector3 direction =
            FlatDirection(
                desiredDirection
            );

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                GetWanderDirection();
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                GetDirectionTowardFieldCenter();
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector3.forward;
        }


        direction =
            ApplyBoundarySteering(
                direction
            );


        direction =
            ApplyObstacleSteering(
                direction
            );


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                GetDirectionTowardFieldCenter();
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                Vector3.forward;
        }


        return
            direction.normalized;
    }


    // ==================================================
    // FLEE DIRECTION
    // ==================================================

    public Vector3 GetFleeDirection(
        Tornado danger)
    {
        if (tornado == null ||
            danger == null)
        {
            return
                GetWanderDirection();
        }


        Vector3 away =
            FlatDirection(
                tornado.transform.position -
                danger.transform.position
            );


        Vector3 center =
            GetDirectionTowardFieldCenter();


        Vector3 direction =
            FlatDirection(
                away *
                1.65f +
                center *
                0.45f
            );


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                away;
        }


        if (direction.sqrMagnitude <
            0.001f)
        {
            direction =
                center;
        }


        return
            BuildMovementDirection(
                direction
            );
    }


    // ==================================================
    // TARGET DIRECTION
    // ==================================================

    public Vector3 GetDirectionToward(
        Transform target)
    {
        if (tornado == null ||
            target == null)
        {
            return
                GetWanderDirection();
        }


        Vector3 direction =
            target.position -
            tornado.transform.position;


        return
            BuildMovementDirection(
                direction
            );
    }


    // ==================================================
    // BOUNDARY STEERING
    // ==================================================

    private Vector3 ApplyBoundarySteering(
        Vector3 desiredDirection)
    {
        PlayingField field =
            PlayingField.Instance;


        if (field == null ||
            tornado == null)
        {
            return
                desiredDirection;
        }


        Vector3 direction =
            FlatDirection(
                desiredDirection
            );


        float clearance =
            GetNavigationRadius();


        Vector3 probe =
            tornado.transform.position +
            direction *
            TornadoAIBalance.BoundaryLookAhead;


        if (field.IsInside(
                probe,
                clearance))
        {
            return
                direction;
        }


        Vector3 towardCenter =
            GetDirectionTowardFieldCenter();


        // Once the look-ahead crosses the safe bounds, turn inward
        // decisively. Blending here allowed outward intent to survive and
        // made agents visibly grind against the movement clamp.
        return
            towardCenter.sqrMagnitude > 0.001f
                ? towardCenter
                : -direction;
    }


    // ==================================================
    // OBSTACLE STEERING
    // ==================================================

    private Vector3 ApplyObstacleSteering(
        Vector3 desiredDirection)
    {
        if (tornado == null)
        {
            return
                desiredDirection;
        }


        Vector3 direction =
            FlatDirection(
                desiredDirection
            );


        if (direction.sqrMagnitude <
            0.001f)
        {
            return
                direction;
        }


        float radius =
            GetNavigationRadius();


        float lookAhead =
            TornadoAIBalance.ObstacleLookAhead +
            radius;


        if (IsDirectionClear(
                direction,
                lookAhead,
                radius))
        {
            return
                direction;
        }

        Vector3 centerDirection =
            GetDirectionTowardFieldCenter();


        Vector3 bestDirection =
            Vector3.zero;


        float bestScore =
            float.NegativeInfinity;


        // Try increasingly decisive turns on both sides. A clear route wins;
        // alignment with the original goal and field center breaks ties.
        for (int step = 1;
             step <= 5;
             step++)
        {
            float angle =
                step * 30f;


            for (int side = -1;
                 side <= 1;
                 side += 2)
            {
                Vector3 candidate =
                    Quaternion.Euler(
                        0f,
                        angle * side,
                        0f
                    ) * direction;


                candidate =
                    FlatDirection(candidate);


                if (!IsDirectionClear(
                        candidate,
                        lookAhead,
                        radius))
                {
                    continue;
                }


                float score =
                    Vector3.Dot(
                        candidate,
                        direction
                    ) * 2f +
                    Vector3.Dot(
                        candidate,
                        centerDirection
                    ) * 0.35f;


                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = candidate;
                }
            }


            if (bestDirection.sqrMagnitude >
                0.001f)
            {
                return bestDirection.normalized;
            }
        }


        // Completely boxed in: turn around instead of continuously pushing
        // into the obstacle while waiting for stuck recovery.
        Vector3 retreat =
            FlatDirection(
                -direction +
                centerDirection * 0.25f
            );


        return
            retreat.sqrMagnitude > 0.001f
                ? retreat.normalized
                : -direction;
    }


    private float GetNavigationRadius()
    {
        if (tornado == null)
        {
            return
                TornadoAIBalance.ObstacleProbeRadius;
        }


        return
            Mathf.Max(
                TornadoAIBalance.ObstacleProbeRadius,
                1.68f * tornado.VisualScale + 0.15f
            );
    }


    private bool IsDirectionClear(
        Vector3 direction,
        float distance,
        float radius)
    {
        Vector3 origin =
            tornado.transform.position;


        for (int step = 1;
             step <= 3;
             step++)
        {
            Vector3 probe =
                origin +
                direction *
                distance *
                (step / 3f);


            if (IsBlockedPosition(
                    probe,
                    radius))
            {
                return false;
            }
        }


        return true;
    }


    // ==================================================
    // BLOCKING
    // ==================================================

    public bool IsBlockedPosition(
        Vector3 position,
        float radius)
    {
        PlayingField field =
            PlayingField.Instance;


        if (field != null &&
            !field.IsInside(
                position,
                radius))
        {
            return true;
        }


        if (tornado == null)
        {
            return false;
        }


        var destructibles =
            TornadoDestructible.ActiveDestructibles;


        for (int i = 0;
             i < destructibles.Count;
             i++)
        {
            TornadoDestructible destructible =
                destructibles[i];


            if (destructible == null ||
                !destructible.isActiveAndEnabled)
            {
                continue;
            }


            /*
             * IMPORTANT:
             *
             * Preserve the existing environment rule.
             *
             * If this tornado is already large enough
             * to destroy the object, that object must
             * NOT be treated as a navigation blocker.
             */
            if (destructible.IsTornadoBigEnough(
                    tornado))
            {
                continue;
            }


            if (destructible.BlocksTornadoPosition(
                    position,
                    radius))
            {
                return true;
            }
        }


        return false;
    }


    // ==================================================
    // CANDY PATH CHECK
    // ==================================================

    public bool IsCandyPathBlocked(
        Candy candy)
    {
        if (candy == null ||
            tornado == null)
        {
            return false;
        }


        Vector3 start =
            tornado.transform.position;


        Vector3 end =
            candy.transform.position;


        start.y =
            0f;

        end.y =
            0f;


        Vector3 difference =
            end -
            start;


        float distance =
            difference.magnitude;


        if (distance <
            0.01f)
        {
            return false;
        }


        Vector3 direction =
            difference /
            distance;


        /*
         * Probe several points along the route rather
         * than only checking the candy position.
         *
         * This catches candy sitting behind a building
         * without needing physics raycasts or layers.
         */
        const int ProbeCount = 4;


        for (int i = 1;
             i <= ProbeCount;
             i++)
        {
            float t =
                i /
                (float)(ProbeCount + 1);


            Vector3 probe =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );


            probe +=
                direction *
                0.05f;


            if (IsBlockedPosition(
                    probe,
                    TornadoAIBalance.ObstacleProbeRadius))
            {
                return true;
            }
        }


        return false;
    }


    // ==================================================
    // STUCK DETECTION
    // ==================================================

    public bool UpdateStuckDetection()
    {
        if (tornado == null)
        {
            return false;
        }


        if (Time.time <
            nextStuckCheckTime)
        {
            return false;
        }


        nextStuckCheckTime =
            Time.time +
            TornadoAIBalance.StuckCheckInterval;


        Vector3 current =
            tornado.transform.position;


        float moved =
            GetFlatDistance(
                current,
                lastStuckPosition
            );


        lastStuckPosition =
            current;


        if (combat != null &&
            combat.IsCaptured)
        {
            failedMovementChecks =
                0;

            return false;
        }


        if (moved <
            TornadoAIBalance.MinimumMovementForStuckCheck)
        {
            failedMovementChecks++;
        }
        else
        {
            failedMovementChecks =
                0;
        }


        if (failedMovementChecks <
            TornadoAIBalance.StuckChecksBeforeRecovery)
        {
            return false;
        }


        failedMovementChecks =
            0;


        BeginRecovery();


        return true;
    }


    // ==================================================
    // RECOVERY
    // ==================================================

    public void BeginRecovery()
    {
        if (tornado == null)
        {
            return;
        }


        Vector3 centerDirection =
            GetDirectionTowardFieldCenter();


        Vector2 random2D =
            Random.insideUnitCircle;


        if (random2D.sqrMagnitude <
            0.05f)
        {
            random2D =
                Vector2.right;
        }


        random2D.Normalize();


        Vector3 random =
            new Vector3(
                random2D.x,
                0f,
                random2D.y
            );


        recoveryDirection =
            FlatDirection(
                centerDirection *
                1.15f +
                random *
                TornadoAIBalance.RecoveryTurnStrength
            );


        if (recoveryDirection.sqrMagnitude <
            0.01f)
        {
            recoveryDirection =
                -FlatDirection(
                    tornado.transform.forward
                );
        }


        if (recoveryDirection.sqrMagnitude <
            0.01f)
        {
            recoveryDirection =
                Vector3.forward;
        }


        recoveryUntil =
            Time.time +
            TornadoAIBalance.RecoveryDuration;


        tornado.SetMoveIntent(
            recoveryDirection,
            1f
        );
    }


    public void CancelRecovery()
    {
        recoveryUntil =
            0f;
    }


    // ==================================================
    // FIELD CENTER
    // ==================================================

    public Vector3 GetDirectionTowardFieldCenter()
    {
        if (tornado == null)
        {
            return
                Vector3.zero;
        }


        PlayingField field =
            PlayingField.Instance;


        if (field == null)
        {
            return
                FlatDirection(
                    -tornado.transform.position
                );
        }


        Vector3 center =
            field.PlayableBounds.center;


        center.y =
            tornado.transform.position.y;


        return
            FlatDirection(
                center -
                tornado.transform.position
            );
    }


    // ==================================================
    // HELPERS
    // ==================================================

    public static Vector3 FlatDirection(
        Vector3 direction)
    {
        direction.y =
            0f;


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return
                Vector3.zero;
        }


        return
            direction.normalized;
    }


    public static float GetFlatDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y =
            0f;

        b.y =
            0f;


        return
            Vector3.Distance(
                a,
                b
            );
    }
}