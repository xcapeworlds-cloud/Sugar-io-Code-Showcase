using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado))]
public sealed class TornadoCombat : MonoBehaviour
{
    // ==================================================
    // INTERNAL PHYSICAL SETTINGS
    // ==================================================

    // Matches the widest radius of TornadoVFX at visual scale 1.
    private const float RadiusAtScaleOne = 1.68f;
    private const float MinimumRadius = 0.52f;

    // ==================================================
    // FALLBACK BALANCE
    // ==================================================

    private const float FallbackRequiredOverlap = 0.25f;
    private const float FallbackPlayerGraceTime = 0.40f;
    private const float FallbackConsumeDuration = 0.24f;
    private const float FallbackCapturePadding = 3.15f;
    private const float FallbackSugarTransferMultiplier = 0.30f;

    // ==================================================
    // RUNTIME
    // ==================================================

    private Tornado tornado;
    private Tornado captor;
    private Tornado pendingPredator;

    private float pendingTime;
    private float consumeTime;
    private float capturedSugar;

    private Color capturedColor;

    private Vector3 startPosition;
    private Vector3 startScale;

    public bool IsCaptured { get; private set; }

    // ==================================================
    // BALANCE
    // ==================================================

    private static MatchBalance GetBalance()
    {
        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        return
            match != null
                ? match.Balance
                : null;
    }

    private static float GetRequiredOverlap()
    {
        MatchBalance balance =
            GetBalance();

        return
            balance != null
                ? balance.combatRequiredOverlap
                : FallbackRequiredOverlap;
    }

    private static float GetPlayerGraceTime()
    {
        MatchBalance balance =
            GetBalance();

        return
            balance != null
                ? balance.playerCaptureGraceTime
                : FallbackPlayerGraceTime;
    }

    private static float GetConsumeDuration()
    {
        MatchBalance balance =
            GetBalance();

        return
            balance != null
                ? balance.tornadoConsumeDuration
                : FallbackConsumeDuration;
    }

    private static float GetCapturePadding()
    {
        MatchBalance balance =
            GetBalance();

        return
            balance != null
                ? balance.combatCapturePadding
                : FallbackCapturePadding;
    }

    private static float GetSugarTransferMultiplier()
    {
        MatchBalance balance =
            GetBalance();

        return
            balance != null
                ? balance.tornadoSugarTransferMultiplier
                : FallbackSugarTransferMultiplier;
    }

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        tornado =
            GetComponent<Tornado>();
    }

    private void FixedUpdate()
    {
        if (tornado == null ||
            tornado.IsDefeated)
        {
            return;
        }

        TornadoMatchManager match =
            TornadoMatchManager.Instance;

        if (match != null &&
            !match.IsPlaying)
        {
            if (IsCaptured)
            {
                ReleaseCapture();
            }

            ClearPending();
            return;
        }

        if (IsCaptured)
        {
            UpdateCapture();
            return;
        }

        Tornado predator =
            FindOverlappingPredator();

        if (predator == null)
        {
            ClearPending();
            return;
        }

        bool playerNeedsGrace =
            !tornado.IsAI &&
            predator.IsAI;

        if (!playerNeedsGrace)
        {
            BeginCapture(predator);
            return;
        }

        if (pendingPredator != predator)
        {
            pendingPredator =
                predator;

            pendingTime =
                0f;
        }

        pendingTime +=
            Time.fixedDeltaTime;

        if (pendingTime >=
            GetPlayerGraceTime())
        {
            BeginCapture(predator);
        }
    }

    // ==================================================
    // PREDATOR SEARCH
    // ==================================================

    private Tornado FindOverlappingPredator()
    {
        Tornado best =
            null;

        float bestDistance =
            float.MaxValue;

        var tornadoes =
            Tornado.ActiveTornadoes;

        for (int i = 0;
             i < tornadoes.Count;
             i++)
        {
            Tornado candidate =
                tornadoes[i];

            if (candidate == null ||
                candidate == tornado ||
                candidate.IsDefeated ||
                candidate.IsFinishingConsumption ||
                !candidate.CanConsume(tornado))
            {
                continue;
            }

            float distance =
                FlatDistanceSquared(
                    candidate.transform.position,
                    tornado.transform.position
                );

            float captureDistance =
                GetCaptureDistance(
                    candidate,
                    tornado
                );

            if (distance <=
                    captureDistance *
                    captureDistance &&
                distance <
                    bestDistance)
            {
                bestDistance =
                    distance;

                best =
                    candidate;
            }
        }

        return best;
    }

    // ==================================================
    // CAPTURE DISTANCE
    // ==================================================

    private static float GetCaptureDistance(
        Tornado predator,
        Tornado victim)
    {
        float predatorRadius =
            Mathf.Max(
                MinimumRadius,
                RadiusAtScaleOne *
                predator.VisualScale
            );

        float victimRadius =
            Mathf.Max(
                MinimumRadius,
                RadiusAtScaleOne *
                victim.VisualScale
            );

        float requiredOverlap =
            GetRequiredOverlap();

        return
            predatorRadius +
            victimRadius -
            victimRadius *
            2f *
            requiredOverlap +
            GetCapturePadding();
    }

    // ==================================================
    // CAPTURE
    // ==================================================

    private void BeginCapture(
        Tornado predator)
    {
        if (IsCaptured ||
            predator == null ||
            !predator.CanConsume(tornado))
        {
            return;
        }

        IsCaptured =
            true;

        captor =
            predator;

        consumeTime =
            0f;

        startPosition =
            tornado.transform.position;

        startScale =
            tornado.transform.localScale;

        capturedSugar =
            tornado.Sugar;

        capturedColor =
            tornado.Colors != null
                ? tornado.Colors.GetMainColor()
                : Color.white;

        tornado.SetMoveIntent(
            Vector3.zero,
            0f
        );

        ClearPending();
    }

    private void UpdateCapture()
    {
        if (captor == null ||
            captor.IsDefeated)
        {
            ReleaseCapture();
            return;
        }

        consumeTime +=
            Time.fixedDeltaTime;

        float duration =
            Mathf.Max(
                0.01f,
                GetConsumeDuration()
            );

        float progress =
            Mathf.Clamp01(
                consumeTime /
                duration
            );

        float suction =
            progress *
            progress;

        Vector3 target =
            captor.BottomTipWorldPosition +
            Vector3.up *
            captor.GetVisibleHeight() *
            0.12f;

        Vector3 currentPosition =
            tornado.transform.position;

        Vector3 desiredPosition =
            Vector3.Lerp(
                startPosition,
                target,
                suction
            );

        tornado.SetCombatPosition(
            Vector3.Lerp(
                currentPosition,
                desiredPosition,
                0.72f
            )
        );

        float scaleMultiplier =
            Mathf.Lerp(
                1f,
                0.02f,
                suction
            );

        tornado.transform.localScale =
            startScale *
            scaleMultiplier;

        if (progress >= 1f)
        {
            FinishCapture();
        }
    }

    private void FinishCapture()
    {
        Tornado winner =
            captor;

        float reward =
            Mathf.Max(
                1f,
                capturedSugar *
                Mathf.Clamp01(
                    GetSugarTransferMultiplier()
                )
            );

        IsCaptured =
            false;

        captor =
            null;

        if (winner != null &&
            !winner.IsDefeated)
        {
            winner.AddSugar(
                reward,
                capturedColor
            );
        }

        if (tornado != null &&
            !tornado.IsDefeated)
        {
            tornado.Defeat(
                winner,
                reward
            );
        }
    }

    private void ReleaseCapture()
    {
        IsCaptured =
            false;

        captor =
            null;

        consumeTime =
            0f;

        if (tornado != null &&
            !tornado.IsDefeated)
        {
            tornado.transform.localScale =
                startScale;
        }
    }

    private void ClearPending()
    {
        pendingPredator =
            null;

        pendingTime =
            0f;
    }

    // ==================================================
    // DISTANCE
    // ==================================================

    private static float FlatDistanceSquared(
        Vector3 a,
        Vector3 b)
    {
        float x =
            a.x - b.x;

        float z =
            a.z - b.z;

        return
            x * x +
            z * z;
    }
}