using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado))]
public sealed class TornadoAI : MonoBehaviour
{
    private enum State
    {
        Wander,
        Collect,
        Environment,
        Hunt,
        Flee
    }

    private Tornado tornado;
    private TornadoCombat combat;
    private TornadoAINavigation navigation;

    private State state;
    private Candy candyTarget;
    private TornadoDestructible environmentTarget;
    private Tornado tornadoTarget;

    private float nextThinkTime;
    private float playerHuntAllowedAt;
    private float blockedCandyUntil;
    private Candy blockedCandy;

    private float candyVacuumMultiplier = 0.8f;
    private float aggression = 1f;
    private float caution = 1f;
    private bool prefersCandy;

    private readonly List<Candy> nearbyCandy =
        new List<Candy>(64);

    public float CandyVacuumMultiplier =>
        candyVacuumMultiplier;

    private MatchBalance Balance
    {
        get
        {
            TornadoMatchManager match =
                TornadoMatchManager.Instance;

            return match != null
                ? match.Balance
                : null;
        }
    }

    private void Awake()
    {
        tornado = GetComponent<Tornado>();
        combat = GetComponent<TornadoCombat>();
        navigation = new TornadoAINavigation(
            tornado,
            combat
        );
    }

    private void OnEnable()
    {
        state = State.Wander;
        candyTarget = null;
        environmentTarget = null;
        tornadoTarget = null;
        blockedCandy = null;
        blockedCandyUntil = 0f;
        // AI instances keep the same think rate, but do not all perform
        // target searches on the exact same frame.
        nextThinkTime =
            Time.time +
            Mathf.Abs(GetInstanceID() % 1000) /
            1000f * TornadoAIBalance.ThinkInterval;

        navigation?.ResetRuntime();
        ResetPlayerSafeTime();
    }

    public void ConfigurePersonality(int index)
    {
        int type = Mathf.Abs(index) % 3;
        prefersCandy = type == 0;

        // Small, deterministic differences make opponents feel distinct
        // without changing their movement speed or gameplay rules.
        aggression = type == 0 ? 0.85f : type == 1 ? 1f : 1.15f;
        caution = type == 0 ? 1.15f : type == 1 ? 1f : 0.9f;

        MatchBalance balance = Balance;

        candyVacuumMultiplier = prefersCandy
            ? (balance != null
                ? balance.aiCollectorVacuumMultiplier
                : TornadoAIBalance.FallbackCollectorVacuumMultiplier)
            : (balance != null
                ? balance.aiHunterVacuumMultiplier
                : TornadoAIBalance.FallbackHunterVacuumMultiplier);

        gameObject.name = type switch
        {
            0 => "AI Tornado (Collector)",
            1 => "AI Tornado (Balanced)",
            _ => "AI Tornado (Hunter)"
        };
    }

    private void Update()
    {
        if (tornado == null || tornado.IsDefeated)
        {
            return;
        }

        TornadoMatchManager match = TornadoMatchManager.Instance;

        if ((match != null && !match.IsPlaying) ||
            (combat != null && combat.IsCaptured))
        {
            tornado.SetMoveIntent(Vector3.zero, 0f);
            navigation?.ResetStuckDetection();
            return;
        }

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime =
                Time.time + TornadoAIBalance.ThinkInterval;

            ChooseAction();
        }

        Move();

        if (navigation != null &&
            navigation.UpdateStuckDetection())
        {
            if (candyTarget != null)
            {
                blockedCandy = candyTarget;
                blockedCandyUntil =
                    Time.time + TornadoAIBalance.BlockedCandyCooldown;
            }

            ClearTargets();
            navigation.BeginRecovery();
        }
    }

    private void ChooseAction()
    {
        Tornado danger = FindDanger();

        if (danger != null)
        {
            state = State.Flee;
            tornadoTarget = danger;
            candyTarget = null;
            environmentTarget = null;
            return;
        }

        if (TryGetFinalOpponent(out Tornado finalOpponent))
        {
            if (tornado.CanConsume(finalOpponent))
            {
                SetHunt(finalOpponent);
                return;
            }

            Candy finalCandy = FindBestCandy();

            if (finalCandy != null)
            {
                SetCandy(finalCandy);
                return;
            }

            SetHunt(finalOpponent);
            return;
        }

        float earlySugar = GetEarlyGameSugar();

        // Normal Hole.io-style opening: grow first. No opportunistic combat
        // bypasses this branch.
        if (tornado.Sugar < earlySugar)
        {
            TornadoDestructible earlyEnvironment =
                FindBestEnvironment();

            if (earlyEnvironment != null)
            {
                SetEnvironment(earlyEnvironment);
                return;
            }

            Candy earlyCandy = FindBestCandy();

            if (earlyCandy != null)
            {
                SetCandy(earlyCandy);
            }
            else
            {
                SetWander();
            }

            return;
        }

        Tornado prey = FindBestPrey();
        Candy candy = FindBestCandy();
        TornadoDestructible environment =
            FindBestEnvironment();
        bool lateGame = tornado.Sugar >= GetAggressiveSugar();

        if (prey != null && ShouldHunt(prey, candy, lateGame))
        {
            SetHunt(prey);
        }
        else if (candy != null)
        {
            if (environment != null &&
                (prefersCandy ||
                 environment.RemainingSugar >= candy.SugarValue))
            {
                SetEnvironment(environment);
            }
            else
            {
                SetCandy(candy);
            }
        }
        else if (environment != null)
        {
            SetEnvironment(environment);
        }
        else if (prey != null)
        {
            SetHunt(prey);
        }
        else
        {
            SetWander();
        }
    }

    private bool ShouldHunt(
        Tornado prey,
        Candy candy,
        bool lateGame)
    {
        if (prey == null)
        {
            return false;
        }

        float preyDistance = FlatDistance(
            transform.position,
            prey.transform.position
        );

        if (lateGame)
        {
            return !prefersCandy ||
                   candy == null ||
                   preyDistance <= 8f * aggression;
        }

        if (prefersCandy && candy != null)
        {
            return false;
        }

        return preyDistance <= 12f * aggression;
    }

    private Candy FindBestCandy()
    {
        float range = GetCandyRange();
        float rangeSquared = range * range;
        Vector3 position = transform.position;

        Candy.GetNearbyNonAlloc(
            position,
            range,
            nearbyCandy
        );

        Candy best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < nearbyCandy.Count; i++)
        {
            Candy candidate = nearbyCandy[i];

            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                candidate.IsBeingAbsorbed ||
                !candidate.CanBeEatenBy(tornado) ||
                (candidate == blockedCandy &&
                 Time.time < blockedCandyUntil))
            {
                continue;
            }

            Vector3 difference =
                candidate.transform.position - position;
            difference.y = 0f;

            float distanceSquared = difference.sqrMagnitude;

            if (distanceSquared > rangeSquared)
            {
                continue;
            }

            float value = Mathf.Max(1f, candidate.SugarValue);
            float score = distanceSquared / Mathf.Sqrt(value);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private TornadoDestructible FindBestEnvironment()
    {
        var destructibles =
            TornadoDestructible.ActiveDestructibles;

        float range = GetCandyRange();
        float rangeSquared = range * range;
        Vector3 position = transform.position;

        TornadoDestructible best = null;
        float bestScore = float.MaxValue;

        for (int i = 0;
             i < destructibles.Count;
             i++)
        {
            TornadoDestructible candidate =
                destructibles[i];

            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                candidate.IsActivated ||
                candidate.IsWaitingForRespawn ||
                candidate.RemainingSugar <= 0f ||
                !candidate.IsTornadoBigEnough(tornado))
            {
                continue;
            }

            Vector3 difference =
                candidate.transform.position - position;
            difference.y = 0f;

            float distanceSquared =
                difference.sqrMagnitude;

            if (distanceSquared > rangeSquared)
            {
                continue;
            }

            float value =
                Mathf.Max(
                    1f,
                    candidate.RemainingSugar
                );

            float score =
                distanceSquared /
                Mathf.Sqrt(value);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private Tornado FindBestPrey()
    {
        var tornadoes = Tornado.ActiveTornadoes;
        float huntRange = GetHuntRange();
        Vector3 position = transform.position;

        Tornado best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < tornadoes.Count; i++)
        {
            Tornado candidate = tornadoes[i];

            if (candidate == null ||
                candidate == tornado ||
                candidate.IsDefeated ||
                !candidate.isActiveAndEnabled ||
                !tornado.CanConsume(candidate))
            {
                continue;
            }

            bool isPlayer = !candidate.IsAI;

            if (isPlayer && Time.time < playerHuntAllowedAt)
            {
                continue;
            }

            float allowedRange = isPlayer
                ? Mathf.Min(huntRange, GetPlayerHuntRange())
                : huntRange;

            float distance = FlatDistance(
                position,
                candidate.transform.position
            );

            if (distance > allowedRange)
            {
                continue;
            }

            // Prefer close prey, with a mild preference for clearer wins.
            float sizeAdvantage = tornado.VisualScale /
                Mathf.Max(0.01f, candidate.VisualScale);
            float score = distance / Mathf.Max(1f, sizeAdvantage);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private Tornado FindDanger()
    {
        var tornadoes = Tornado.ActiveTornadoes;
        float range = TornadoAIBalance.FleeDetectionRange * caution;
        Tornado nearest = null;
        float nearestDistance = range;

        for (int i = 0; i < tornadoes.Count; i++)
        {
            Tornado candidate = tornadoes[i];

            if (candidate == null ||
                candidate == tornado ||
                candidate.IsDefeated ||
                !candidate.CanConsume(tornado))
            {
                continue;
            }

            float distance = FlatDistance(
                transform.position,
                candidate.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool TryGetFinalOpponent(out Tornado opponent)
    {
        opponent = null;
        int alive = 0;
        var tornadoes = Tornado.ActiveTornadoes;

        for (int i = 0; i < tornadoes.Count; i++)
        {
            Tornado candidate = tornadoes[i];

            if (candidate == null ||
                candidate.IsDefeated ||
                !candidate.isActiveAndEnabled)
            {
                continue;
            }

            alive++;

            if (candidate != tornado)
            {
                opponent = candidate;
            }

            if (alive > 2)
            {
                opponent = null;
                return false;
            }
        }

        return alive == 2 && opponent != null;
    }

    private void Move()
    {
        if (navigation == null)
        {
            return;
        }

        Vector3 direction;
        float throttle = GetWanderThrottle();

        if (navigation.IsRecovering)
        {
            direction = navigation.BuildMovementDirection(
                navigation.RecoveryDirection
            );
            throttle = 1f;
        }
        else if (state == State.Flee && IsValid(tornadoTarget))
        {
            direction = navigation.GetFleeDirection(tornadoTarget);
            throttle = 1f;
        }
        else if (state == State.Hunt && IsValid(tornadoTarget))
        {
            direction = navigation.GetDirectionToward(
                tornadoTarget.transform
            );
            throttle = GetHuntThrottle();
        }
        else if (state == State.Environment &&
                 IsValid(environmentTarget))
        {
            direction = navigation.GetDirectionToward(
                environmentTarget.transform
            );
        }
        else if (state == State.Collect &&
                 candyTarget != null &&
                 candyTarget.isActiveAndEnabled &&
                 !candyTarget.IsBeingAbsorbed &&
                 candyTarget.CanBeEatenBy(tornado))
        {
            if (navigation.IsCandyPathBlocked(candyTarget))
            {
                blockedCandy = candyTarget;
                blockedCandyUntil =
                    Time.time + TornadoAIBalance.BlockedCandyCooldown;
                SetWander();
                navigation.BeginRecovery();
                direction = navigation.RecoveryDirection;
            }
            else
            {
                direction = navigation.GetDirectionToward(
                    candyTarget.transform
                );
            }
        }
        else
        {
            SetWander();
            direction = navigation.GetWanderDirection();
        }

        tornado.SetMoveIntent(direction, throttle);
    }

    private void SetCandy(Candy target)
    {
        state = State.Collect;
        candyTarget = target;
        environmentTarget = null;
        tornadoTarget = null;
    }

    private void SetEnvironment(
        TornadoDestructible target)
    {
        state = State.Environment;
        environmentTarget = target;
        candyTarget = null;
        tornadoTarget = null;
    }

    private void SetHunt(Tornado target)
    {
        state = State.Hunt;
        tornadoTarget = target;
        candyTarget = null;
        environmentTarget = null;
    }

    private void SetWander()
    {
        state = State.Wander;
        candyTarget = null;
        environmentTarget = null;
        tornadoTarget = null;
    }

    private void ClearTargets()
    {
        state = State.Wander;
        candyTarget = null;
        environmentTarget = null;
        tornadoTarget = null;
    }

    private void ResetPlayerSafeTime()
    {
        MatchBalance balance = Balance;
        float safeTime = balance != null
            ? balance.aiPlayerSafeTime
            : TornadoAIBalance.FallbackPlayerSafeTime;

        playerHuntAllowedAt = Time.time + safeTime;
    }

    private float GetCandyRange()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiCandyRange
            : TornadoAIBalance.FallbackCandyRange;
    }

    private float GetHuntRange()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiHuntRange
            : TornadoAIBalance.FallbackHuntRange;
    }

    private float GetPlayerHuntRange()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiPlayerHuntRange
            : TornadoAIBalance.FallbackPlayerHuntRange;
    }

    private float GetEarlyGameSugar()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiEarlyGameSugar
            : TornadoAIBalance.FallbackEarlyGameSugar;
    }

    private float GetAggressiveSugar()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiAggressiveSugar
            : TornadoAIBalance.FallbackAggressiveSugar;
    }

    private float GetWanderThrottle()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiWanderThrottle
            : TornadoAIBalance.FallbackWanderThrottle;
    }

    private float GetHuntThrottle()
    {
        MatchBalance balance = Balance;
        return balance != null
            ? balance.aiHuntThrottle
            : TornadoAIBalance.FallbackHuntThrottle;
    }

    private static bool IsValid(Tornado candidate)
    {
        return candidate != null &&
               candidate.isActiveAndEnabled &&
               !candidate.IsDefeated;
    }

    private static bool IsValid(
        TornadoDestructible candidate)
    {
        return candidate != null &&
               candidate.isActiveAndEnabled &&
               !candidate.IsWaitingForRespawn &&
               !candidate.IsActivated;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}