using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado))]
public sealed class TornadoEnvironmentSuction : MonoBehaviour
{
    // =========================================================
    // RANGE
    // =========================================================

    private const float BaseBreakRadius = 5.5f;
    private const float BaseWarningRadius = 7.25f;

    private const float BreakRadiusGrowthPerScale = 0.28f;
    private const float WarningRadiusGrowthPerScale = 0.38f;

    private const float MaximumBreakRadius = 8.5f;
    private const float MaximumWarningRadius = 11f;

    private const float SearchInterval = 0.10f;

    // =========================================================
    // SIZE WARNING
    // =========================================================

    private const float WiggleStartRatio = 0.68f;

    private const float WiggleMaximumAngle = 6.5f;
    private const float WiggleMaximumOffset = 0.12f;
    private const float WiggleSpeed = 11.5f;

    private const float MaximumSizeWiggleMultiplier = 1.30f;

    // =========================================================
    // CRUMBLE
    // =========================================================

    private const float FallbackCrumbleDuration = 0.22f;
    private const float FallbackCrumbleDistance = 0.58f;
    private const float FallbackCrumbleDrop = 0.25f;

    // =========================================================
    // SUCTION
    // =========================================================

    private const float FallbackSuctionDuration = 0.82f;

    private const float SpiralTurns = 1.55f;
    private const float SpinSpeed = 460f;

    private const float FunnelRadiusMultiplier = 0.92f;
    private const float TopHeightMultiplier = 7.0f;

    private const int MaxFlyingPieces = 24;

    // =========================================================
    // OWNERSHIP
    // =========================================================

    private static readonly Dictionary<
        TornadoDestructible,
        TornadoEnvironmentSuction>
        owners = new();

    // =========================================================
    // FLYING PIECE
    // =========================================================

    private sealed class FlyingPiece
    {
        public Transform piece;
        public TornadoDestructible destructible;

        public Vector3 startPosition;
        public Quaternion startRotation;
        public Vector3 startScale;

        public Vector3 crumbleDirection;

        public float startAngle;
        public float startRadius;
        public float timer;
    }

    // =========================================================
    // WIGGLE
    // =========================================================

    private sealed class WiggleTarget
    {
        public TornadoDestructible destructible;

        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;

        public float amount;
        public float phase;
    }

    // =========================================================
    // RUNTIME
    // =========================================================

    private readonly List<FlyingPiece> flyingPieces =
        new(MaxFlyingPieces);

    private readonly List<Transform> pieceBuffer =
        new(48);

    private readonly List<WiggleTarget> wiggleTargets =
        new(16);

    private readonly List<TornadoDestructible> ownerCleanup =
        new(16);

    private Tornado tornado;

    private float nextSearchTime;

    private static MatchBalance GetBalance()
    {
        TornadoMatchManager match = TornadoMatchManager.Instance;
        return match != null ? match.Balance : null;
    }

    private static float GetCrumbleDuration()
    {
        MatchBalance balance = GetBalance();
        return balance != null
            ? Mathf.Max(0.01f, balance.crumbleDuration)
            : FallbackCrumbleDuration;
    }

    private static float GetCrumbleDistance()
    {
        MatchBalance balance = GetBalance();
        return balance != null
            ? Mathf.Max(0f, balance.crumbleDistance)
            : FallbackCrumbleDistance;
    }

    private static float GetCrumbleDrop()
    {
        MatchBalance balance = GetBalance();
        return balance != null
            ? Mathf.Max(0f, balance.crumbleDrop)
            : FallbackCrumbleDrop;
    }

    private static float GetSuctionDuration()
    {
        MatchBalance balance = GetBalance();
        return balance != null
            ? Mathf.Max(0.01f, balance.suctionDuration)
            : FallbackSuctionDuration;
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        tornado =
            GetComponent<Tornado>();

        // Avoid every tornado scanning all destructibles simultaneously.
        nextSearchTime =
            Time.time +
            Mathf.Abs(GetInstanceID() % 1000) /
            1000f * SearchInterval;
    }

    private void Update()
    {
        if (tornado == null ||
            tornado.IsDefeated)
        {
            StopPlayerSuctionAudio();

            RestoreAllFlyingPieces();
            RestoreAllWiggles();
            ReleaseAllOwnedObjects();

            return;
        }

        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (match != null &&
            !match.IsPlaying)
        {
            StopPlayerSuctionAudio();

            if (flyingPieces.Count > 0)
            {
                RestoreAllFlyingPieces();
                ReleaseAllOwnedObjects();
            }

            RestoreAllWiggles();
            return;
        }

        if (Time.time >=
            nextSearchTime)
        {
            nextSearchTime =
                Time.time +
                SearchInterval;

            SearchEnvironment();
        }

        UpdateWiggles();
        UpdateFlyingPieces();
        CleanupOwners();
    }

    private void OnDisable()
    {
        StopPlayerSuctionAudio();

        RestoreAllFlyingPieces();
        RestoreAllWiggles();
        ReleaseAllOwnedObjects();

        pieceBuffer.Clear();
        ownerCleanup.Clear();
    }

    // =========================================================
    // AUDIO
    // =========================================================

    private bool IsHumanPlayer()
    {
        return
            tornado != null &&
            !tornado.IsAI;
    }

    private void StartPlayerSuctionAudio()
    {
        if (!IsHumanPlayer())
        {
            return;
        }

        GameAudioManager.Instance
            ?.StartSuction();
    }

    private void StopPlayerSuctionAudio()
    {
        if (!IsHumanPlayer())
        {
            return;
        }

        GameAudioManager.Instance
            ?.StopSuction();
    }

    // =========================================================
    // RANGE
    // =========================================================

    private float GetBreakRadius()
    {
        float scale =
            Mathf.Max(
                0.1f,
                tornado.VisualScale
            );

        float bonus =
            Mathf.Max(
                0f,
                scale - 1f
            ) *
            BreakRadiusGrowthPerScale;

        return
            Mathf.Min(
                MaximumBreakRadius,
                BaseBreakRadius +
                bonus
            );
    }

    private float GetWarningRadius()
    {
        float scale =
            Mathf.Max(
                0.1f,
                tornado.VisualScale
            );

        float bonus =
            Mathf.Max(
                0f,
                scale - 1f
            ) *
            WarningRadiusGrowthPerScale;

        return
            Mathf.Min(
                MaximumWarningRadius,
                BaseWarningRadius +
                bonus
            );
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private void SearchEnvironment()
    {
        IReadOnlyList<TornadoDestructible> destructibles =
            TornadoDestructible.ActiveDestructibles;

        Vector3 tornadoPosition =
            tornado.transform.position;

        float breakRadius =
            GetBreakRadius();

        float warningRadius =
            GetWarningRadius();

        float breakRadiusSquared =
            breakRadius *
            breakRadius;

        float warningRadiusSquared =
            warningRadius *
            warningRadius;

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

            if (destructible.IsWaitingForRespawn)
            {
                RemoveWiggle(
                    destructible
                );

                ReleaseOwnership(
                    destructible
                );

                continue;
            }

            if (destructible.IsActivated)
            {
                RemoveWiggle(
                    destructible
                );

                if (IsOwner(
                        destructible))
                {
                    ClaimAvailablePieces(
                        destructible
                    );
                }

                continue;
            }

            float distanceSquared =
                GetNearestVisibleDistanceSquared(
                    destructible,
                    tornadoPosition
                );

            if (distanceSquared >
                warningRadiusSquared)
            {
                RemoveWiggle(
                    destructible
                );

                continue;
            }

            float sizeRatio =
                destructible.GetSizeRatio(
                    tornado
                );

            UpdateWarningWiggle(
                destructible,
                sizeRatio,
                distanceSquared,
                warningRadius
            );

            if (distanceSquared >
                breakRadiusSquared)
            {
                continue;
            }

            if (sizeRatio < 1f)
            {
                continue;
            }

            if (!destructible.CanActivate(
                    tornado))
            {
                continue;
            }

            if (HasDifferentOwner(
                    destructible))
            {
                continue;
            }

            RemoveWiggle(
                destructible
            );

            if (!destructible.TryActivate(
                    tornado))
            {
                continue;
            }

            owners[destructible] =
                this;

            if (IsHumanPlayer())
            {
                GameAudioManager.Instance
                    ?.PlayDestruction(
                        destructible
                            .DestructionSoundOverride
                    );
            }

            ClaimAvailablePieces(
                destructible
            );
        }
    }

    // =========================================================
    // NEAREST VISIBLE DISTANCE
    // =========================================================

    private static float GetNearestVisibleDistanceSquared(
        TornadoDestructible destructible,
        Vector3 tornadoPosition)
    {
        if (destructible == null)
        {
            return float.MaxValue;
        }

        Renderer[] renderers =
            destructible.GetCachedRenderers();

        if (renderers == null ||
            renderers.Length == 0)
        {
            Vector3 difference =
                destructible.transform.position -
                tornadoPosition;

            difference.y =
                0f;

            return
                difference.sqrMagnitude;
        }

        float bestDistanceSquared =
            float.MaxValue;

        bool found =
            false;

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

            found =
                true;

            Bounds bounds =
                renderer.bounds;

            float closestX =
                Mathf.Clamp(
                    tornadoPosition.x,
                    bounds.min.x,
                    bounds.max.x
                );

            float closestZ =
                Mathf.Clamp(
                    tornadoPosition.z,
                    bounds.min.z,
                    bounds.max.z
                );

            float x =
                tornadoPosition.x -
                closestX;

            float z =
                tornadoPosition.z -
                closestZ;

            float distanceSquared =
                x * x +
                z * z;

            if (distanceSquared <
                bestDistanceSquared)
            {
                bestDistanceSquared =
                    distanceSquared;

                if (bestDistanceSquared <=
                    0.0001f)
                {
                    return 0f;
                }
            }
        }

        if (found)
        {
            return bestDistanceSquared;
        }

        Vector3 fallback =
            destructible.transform.position -
            tornadoPosition;

        fallback.y =
            0f;

        return
            fallback.sqrMagnitude;
    }

    // =========================================================
    // WARNING WIGGLE
    // =========================================================

    private void UpdateWarningWiggle(
        TornadoDestructible destructible,
        float sizeRatio,
        float distanceSquared,
        float warningRadius)
    {
        if (sizeRatio <
            WiggleStartRatio)
        {
            RemoveWiggle(
                destructible
            );

            return;
        }

        float sizeAmount =
            Mathf.InverseLerp(
                WiggleStartRatio,
                1f,
                Mathf.Min(
                    sizeRatio,
                    1f
                )
            );

        sizeAmount =
            Mathf.SmoothStep(
                0f,
                1f,
                sizeAmount
            );

        float distance =
            Mathf.Sqrt(
                distanceSquared
            );

        float distanceAmount =
            1f -
            Mathf.Clamp01(
                distance /
                Mathf.Max(
                    0.01f,
                    warningRadius
                )
            );

        float distanceStrength =
            Mathf.Lerp(
                0.58f,
                1f,
                distanceAmount
            );

        float tornadoScale =
            Mathf.Max(
                0.1f,
                tornado.VisualScale
            );

        float scaleMultiplier =
            Mathf.Lerp(
                1f,
                MaximumSizeWiggleMultiplier,
                Mathf.Clamp01(
                    (tornadoScale - 1f) /
                    5f
                )
            );

        float amount =
            sizeAmount *
            distanceStrength *
            scaleMultiplier;

        if (sizeRatio >= 1f)
        {
            amount =
                Mathf.Max(
                    amount,
                    0.72f
                );
        }

        SetWiggle(
            destructible,
            Mathf.Clamp01(
                amount
            )
        );
    }

    // =========================================================
    // CLAIM PIECES
    // =========================================================

    private void ClaimAvailablePieces(
        TornadoDestructible destructible)
    {
        if (destructible == null ||
            destructible.IsWaitingForRespawn ||
            !IsOwner(destructible))
        {
            return;
        }

        if (flyingPieces.Count >=
            MaxFlyingPieces)
        {
            return;
        }

        pieceBuffer.Clear();

        destructible.GetBreakablePieces(
            pieceBuffer
        );

        for (int i = 0;
             i < pieceBuffer.Count;
             i++)
        {
            if (flyingPieces.Count >=
                MaxFlyingPieces)
            {
                return;
            }

            Transform piece =
                pieceBuffer[i];

            if (piece == null ||
                IsAlreadyFlying(
                    piece))
            {
                continue;
            }

            if (!destructible.CanConsumePiece(
                    tornado,
                    piece))
            {
                continue;
            }

            StartCrumble(
                destructible,
                piece,
                i
            );
        }
    }

    // =========================================================
    // START CRUMBLE
    // =========================================================

    private void StartCrumble(
        TornadoDestructible destructible,
        Transform piece,
        int pieceIndex)
    {
        if (destructible == null ||
            piece == null ||
            IsAlreadyFlying(
                piece))
        {
            return;
        }

        Vector3 bottom =
            tornado.BottomTipWorldPosition;

        Vector3 offset =
            piece.position -
            bottom;

        offset.y =
            0f;

        Vector3 outward =
            offset;

        if (outward.sqrMagnitude <
            0.001f)
        {
            float angle =
                pieceIndex *
                2.39996323f;

            outward =
                new Vector3(
                    Mathf.Cos(
                        angle),
                    0f,
                    Mathf.Sin(
                        angle)
                );
        }
        else
        {
            outward.Normalize();
        }

        float directionOffset =
            Mathf.Sin(
                pieceIndex *
                2.17f
            ) *
            28f;

        outward =
            Quaternion.Euler(
                0f,
                directionOffset,
                0f
            ) *
            outward;

        FlyingPiece flying =
            new FlyingPiece
            {
                piece =
                    piece,

                destructible =
                    destructible,

                startPosition =
                    piece.position,

                startRotation =
                    piece.rotation,

                startScale =
                    piece.localScale,

                crumbleDirection =
                    outward,

                startAngle =
                    Mathf.Atan2(
                        offset.z,
                        offset.x
                    ),

                startRadius =
                    Mathf.Max(
                        0.2f,
                        offset.magnitude
                    ),

                // A short capped stagger makes child pieces visibly collapse
                // in sequence instead of all snapping into suction together.
                timer =
                    -Mathf.Min(
                        pieceIndex,
                        8
                    ) * 0.035f
            };

        bool wasIdle =
            flyingPieces.Count == 0;

        flyingPieces.Add(
            flying
        );

        if (wasIdle)
        {
            StartPlayerSuctionAudio();
        }
    }

    // =========================================================
    // UPDATE PIECES
    // =========================================================

    private void UpdateFlyingPieces()
    {
        for (int i =
                 flyingPieces.Count - 1;
             i >= 0;
             i--)
        {
            FlyingPiece flying =
                flyingPieces[i];

            if (flying == null ||
                flying.piece == null ||
                flying.destructible == null)
            {
                flyingPieces.RemoveAt(
                    i
                );

                continue;
            }

            if (tornado == null ||
                tornado.IsDefeated)
            {
                RestoreInterruptedPiece(
                    flying
                );

                flyingPieces.RemoveAt(
                    i
                );

                continue;
            }

            if (flying.destructible
                    .IsWaitingForRespawn)
            {
                flyingPieces.RemoveAt(
                    i
                );

                continue;
            }

            if (!IsOwner(
                    flying.destructible))
            {
                RestoreInterruptedPiece(
                    flying
                );

                flyingPieces.RemoveAt(
                    i
                );

                continue;
            }

            flying.timer +=
                Time.deltaTime;

            float crumbleDuration = GetCrumbleDuration();

            if (flying.timer <
                crumbleDuration)
            {
                float progress =
                    Mathf.Clamp01(
                        flying.timer /
                        crumbleDuration
                    );

                AnimateCrumble(
                    flying,
                    progress
                );

                continue;
            }

            float suctionTime =
                flying.timer -
                crumbleDuration;

            float suctionProgress =
                Mathf.Clamp01(
                    suctionTime /
                    GetSuctionDuration()
                );

            AnimateSuction(
                flying,
                suctionProgress
            );

            if (suctionProgress <
                1f)
            {
                continue;
            }

            FinishSuction(
                flying
            );

            flyingPieces.RemoveAt(
                i
            );
        }

        if (flyingPieces.Count == 0)
        {
            StopPlayerSuctionAudio();
        }
    }

    // =========================================================
    // CRUMBLE
    // =========================================================

    private void AnimateCrumble(
        FlyingPiece flying,
        float progress)
    {
        float pop =
            Mathf.Sin(
                progress *
                Mathf.PI *
                0.5f
            );

        Vector3 position =
            flying.startPosition;

        position +=
            flying.crumbleDirection *
            GetCrumbleDistance() *
            pop;

        position.y -=
            GetCrumbleDrop() *
            progress * progress;

        flying.piece.position =
            position;

        float direction =
            Mathf.Sin(
                flying.startAngle *
                3.1f
            ) >= 0f
                ? 1f
                : -1f;

        flying.piece.rotation =
            flying.startRotation *
            Quaternion.Euler(
                progress *
                    28f *
                    direction,

                progress *
                    40f *
                    direction,

                progress *
                    20f
            );
    }

    // =========================================================
    // SUCTION
    // =========================================================

    private void AnimateSuction(
        FlyingPiece flying,
        float progress)
    {
        Vector3 bottom =
            tornado.BottomTipWorldPosition;

        float tornadoScale =
            Mathf.Max(
                0.1f,
                tornado.VisualScale
            );

        float topY =
            bottom.y +
            TopHeightMultiplier *
            tornadoScale;

        float pull =
            1f -
            Mathf.Pow(
                1f - progress,
                2.4f
            );

        float angle =
            flying.startAngle +
            progress *
            SpiralTurns *
            Mathf.PI *
            2f;

        float funnelRadius =
            FunnelRadiusMultiplier *
            tornadoScale;

        float radius;

        if (progress <
            0.70f)
        {
            float enter =
                Mathf.InverseLerp(
                    0f,
                    0.70f,
                    progress
                );

            radius =
                Mathf.Lerp(
                    Mathf.Max(
                        flying.startRadius,
                        funnelRadius
                    ),
                    funnelRadius,
                    enter
                );
        }
        else
        {
            float finish =
                Mathf.InverseLerp(
                    0.70f,
                    1f,
                    progress
                );

            radius =
                Mathf.Lerp(
                    funnelRadius,
                    0.04f,
                    finish *
                    finish
                );
        }

        float x =
            Mathf.Cos(
                angle
            ) *
            radius;

        float z =
            Mathf.Sin(
                angle
            ) *
            radius;

        float startY =
            flying.startPosition.y -
            GetCrumbleDrop();

        float y =
            Mathf.Lerp(
                startY,
                topY,
                Mathf.SmoothStep(
                    0f,
                    1f,
                    pull
                )
            );

        flying.piece.position =
            new Vector3(
                bottom.x + x,
                y,
                bottom.z + z
            );

        flying.piece.Rotate(
            SpinSpeed *
                Time.deltaTime,

            SpinSpeed *
                0.71f *
                Time.deltaTime,

            SpinSpeed *
                0.43f *
                Time.deltaTime,

            Space.World
        );

        float shrink =
            1f;

        if (progress >
            0.80f)
        {
            float shrinkProgress =
                Mathf.InverseLerp(
                    0.80f,
                    1f,
                    progress
                );

            shrink =
                Mathf.Lerp(
                    1f,
                    0.03f,
                    shrinkProgress *
                    shrinkProgress
                );
        }

        flying.piece.localScale =
            flying.startScale *
            shrink;
    }

    // =========================================================
    // FINISH EATING PIECE
    // =========================================================

    private void FinishSuction(
        FlyingPiece flying)
    {
        if (flying == null ||
            flying.destructible == null ||
            flying.piece == null)
        {
            return;
        }

        float sugar =
            flying.destructible
                .ConsumePieceSugar(
                    flying.piece
                );

        if (sugar > 0f &&
            tornado != null &&
            !tornado.IsDefeated)
        {
            tornado.AddSugar(
                sugar,
                Color.white
            );

            TornadoMatchManager.Instance
                ?.ReportEnvironmentConsumed(
                    tornado,
                    sugar,
                    KawaiiColors.RandomColor()
                );
        }

        flying.destructible
            .HideConsumedPiece(
                flying.piece
            );

        if (flying.destructible
                .IsWaitingForRespawn)
        {
            ReleaseOwnership(
                flying.destructible
            );
        }
    }

    // =========================================================
    // WIGGLE
    // =========================================================

    private void SetWiggle(
        TornadoDestructible destructible,
        float amount)
    {
        if (destructible == null)
        {
            return;
        }

        WiggleTarget target =
            FindWiggle(
                destructible
            );

        if (target == null)
        {
            target =
                new WiggleTarget
                {
                    destructible =
                        destructible,

                    originalLocalPosition =
                        destructible
                            .transform
                            .localPosition,

                    originalLocalRotation =
                        destructible
                            .transform
                            .localRotation,

                    phase =
                        Random.Range(
                            0f,
                            Mathf.PI *
                            2f
                        )
                };

            wiggleTargets.Add(
                target
            );

            if (IsHumanPlayer())
            {
                GameAudioManager.Instance
                    ?.PlayWobble(
                        destructible.transform.position
                    );
            }
        }

        target.amount =
            Mathf.Clamp01(
                amount
            );
    }

    private void UpdateWiggles()
    {
        float time =
            Time.time *
            WiggleSpeed;

        for (int i =
                 wiggleTargets.Count - 1;
             i >= 0;
             i--)
        {
            WiggleTarget target =
                wiggleTargets[i];

            if (target == null ||
                target.destructible == null)
            {
                wiggleTargets.RemoveAt(
                    i
                );

                continue;
            }

            if (target.destructible
                    .IsActivated ||
                target.destructible
                    .IsWaitingForRespawn)
            {
                RestoreWiggle(
                    target
                );

                wiggleTargets.RemoveAt(
                    i
                );

                continue;
            }

            float waveA =
                Mathf.Sin(
                    time +
                    target.phase
                );

            float waveB =
                Mathf.Sin(
                    time *
                    1.67f +
                    target.phase *
                    0.73f
                );

            float wave =
                waveA *
                0.72f +
                waveB *
                0.28f;

            float nearReady =
                Mathf.InverseLerp(
                    0.58f,
                    1f,
                    target.amount
                );

            float shake =
                Mathf.Sin(
                    time *
                    2.1f +
                    target.phase *
                    1.31f
                ) *
                nearReady;

            float finalWave =
                wave +
                shake *
                0.15f;

            float angle =
                finalWave *
                WiggleMaximumAngle *
                target.amount;

            float offset =
                finalWave *
                WiggleMaximumOffset *
                target.amount;

            Transform objectTransform =
                target.destructible
                    .transform;

            objectTransform.localPosition =
                target.originalLocalPosition +
                new Vector3(
                    offset,
                    0f,
                    -offset *
                    0.55f
                );

            objectTransform.localRotation =
                target.originalLocalRotation *
                Quaternion.Euler(
                    angle *
                        0.35f,

                    0f,

                    -angle
                );
        }
    }

    private WiggleTarget FindWiggle(
        TornadoDestructible destructible)
    {
        for (int i = 0;
             i < wiggleTargets.Count;
             i++)
        {
            WiggleTarget target =
                wiggleTargets[i];

            if (target != null &&
                target.destructible ==
                destructible)
            {
                return target;
            }
        }

        return null;
    }

    private void RemoveWiggle(
        TornadoDestructible destructible)
    {
        if (destructible == null)
        {
            return;
        }

        for (int i =
                 wiggleTargets.Count - 1;
             i >= 0;
             i--)
        {
            WiggleTarget target =
                wiggleTargets[i];

            if (target == null)
            {
                wiggleTargets.RemoveAt(
                    i
                );

                continue;
            }

            if (target.destructible !=
                destructible)
            {
                continue;
            }

            RestoreWiggle(
                target
            );

            wiggleTargets.RemoveAt(
                i
            );

            return;
        }
    }

    private static void RestoreWiggle(
        WiggleTarget target)
    {
        if (target == null ||
            target.destructible == null)
        {
            return;
        }

        target.destructible
            .transform
            .localPosition =
                target.originalLocalPosition;

        target.destructible
            .transform
            .localRotation =
                target.originalLocalRotation;
    }

    private void RestoreAllWiggles()
    {
        for (int i = 0;
             i < wiggleTargets.Count;
             i++)
        {
            RestoreWiggle(
                wiggleTargets[i]
            );
        }

        wiggleTargets.Clear();
    }

    // =========================================================
    // OWNERSHIP
    // =========================================================

    private bool IsOwner(
        TornadoDestructible destructible)
    {
        return
            destructible != null &&
            owners.TryGetValue(
                destructible,
                out TornadoEnvironmentSuction owner
            ) &&
            owner == this;
    }

    private bool HasDifferentOwner(
        TornadoDestructible destructible)
    {
        if (destructible == null)
        {
            return false;
        }

        if (!owners.TryGetValue(
                destructible,
                out TornadoEnvironmentSuction owner))
        {
            return false;
        }

        return
            owner != null &&
            owner != this;
    }

    private void ReleaseOwnership(
        TornadoDestructible destructible)
    {
        if (destructible == null)
        {
            return;
        }

        if (!owners.TryGetValue(
                destructible,
                out TornadoEnvironmentSuction owner))
        {
            return;
        }

        if (owner == this ||
            owner == null)
        {
            owners.Remove(
                destructible
            );
        }
    }

    private void ReleaseAllOwnedObjects()
    {
        ownerCleanup.Clear();

        foreach (
            KeyValuePair<
                TornadoDestructible,
                TornadoEnvironmentSuction> pair
            in owners)
        {
            if (pair.Value == this ||
                pair.Value == null)
            {
                ownerCleanup.Add(
                    pair.Key
                );
            }
        }

        for (int i = 0;
             i < ownerCleanup.Count;
             i++)
        {
            owners.Remove(
                ownerCleanup[i]
            );
        }

        ownerCleanup.Clear();
    }

    private void CleanupOwners()
    {
        ownerCleanup.Clear();

        foreach (
            KeyValuePair<
                TornadoDestructible,
                TornadoEnvironmentSuction> pair
            in owners)
        {
            if (pair.Key == null ||
                pair.Value == null ||
                pair.Key.IsWaitingForRespawn ||
                (pair.Key.IsFullyConsumed &&
                 !HasFlyingPiecesFor(pair.Key)))
            {
                ownerCleanup.Add(
                    pair.Key
                );
            }
        }

        for (int i = 0;
             i < ownerCleanup.Count;
             i++)
        {
            owners.Remove(
                ownerCleanup[i]
            );
        }

        ownerCleanup.Clear();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private bool IsAlreadyFlying(
        Transform piece)
    {
        for (int i = 0;
             i < flyingPieces.Count;
             i++)
        {
            FlyingPiece flying =
                flyingPieces[i];

            if (flying != null &&
                flying.piece ==
                piece)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasFlyingPiecesFor(
        TornadoDestructible destructible)
    {
        if (destructible == null)
        {
            return false;
        }

        for (int i = 0; i < flyingPieces.Count; i++)
        {
            FlyingPiece flying = flyingPieces[i];

            if (flying != null &&
                flying.destructible == destructible &&
                flying.piece != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void RestoreInterruptedPiece(
        FlyingPiece flying)
    {
        if (flying == null ||
            flying.piece == null)
        {
            return;
        }

        flying.piece.position =
            flying.startPosition;

        flying.piece.rotation =
            flying.startRotation;

        flying.piece.localScale =
            flying.startScale;
    }

    private void RestoreAllFlyingPieces()
    {
        for (int i = 0;
             i < flyingPieces.Count;
             i++)
        {
            RestoreInterruptedPiece(
                flyingPieces[i]
            );
        }

        flyingPieces.Clear();

        StopPlayerSuctionAudio();
    }

    // =========================================================
    // STATIC RESET
    // =========================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        owners.Clear();
    }
}
