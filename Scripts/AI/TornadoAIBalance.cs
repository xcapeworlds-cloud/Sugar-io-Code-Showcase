/// <summary>
/// Small internal constants for AI timing and navigation.
/// Player-facing balance remains in MatchBalance.
/// </summary>
public static class TornadoAIBalance
{
    public const float ThinkInterval = 0.18f;
    public const float WanderChangeTime = 2f;

    public const float FleeDetectionRange = 6.5f;
    public const float BlockedCandyCooldown = 5f;

    public const float BoundaryLookAhead = 3.2f;
    public const float ObstacleLookAhead = 2.6f;
    public const float ObstacleProbeRadius = 0.72f;

    public const float StuckCheckInterval = 0.45f;
    public const float MinimumMovementForStuckCheck = 0.25f;
    public const int StuckChecksBeforeRecovery = 2;
    public const float RecoveryDuration = 1.25f;
    public const float RecoveryTurnStrength = 2.5f;

    public const float FallbackCandyRange = 26f;
    public const float FallbackHuntRange = 25f;
    public const float FallbackPlayerHuntRange = 24f;
    public const float FallbackPlayerSafeTime = 8f;
    public const float FallbackWanderThrottle = 0.72f;
    public const float FallbackHuntThrottle = 1f;
    public const float FallbackEarlyGameSugar = 12f;
    public const float FallbackAggressiveSugar = 20f;
    public const float FallbackCollectorVacuumMultiplier = 0.90f;
    public const float FallbackHunterVacuumMultiplier = 0.72f;
}