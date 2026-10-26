using UnityEngine;

/// <summary>Central balance tuning for the whole game. Edit values here, not in scripts.</summary>
public static class GameConfig
{
    // --- Bird (snappy classic-Flappy arc: strong gravity, moderate flap, clamped fall) ---
    public const float FlapStrength = 14f;
    public const float GravityScale = 4.6f;
    public const float MaxFallSpeed = 13f;
    public const float BirdRotationUp = 30f;
    public const float BirdRotationDown = -80f;
    public const float RotationSpeed = 480f;
    public const float DeathBounceX = -3.5f;

    // --- Pipes ---
    public const float BaseGapSize = 10f;
    public const float MinGapSize = 7.5f;
    public const float PipeWidth = 2.3f;
    public const float BasePipeSpeed = 5f;
    public const float MaxPipeSpeed = 12f;
    public const float SpeedPerPipe = 0.16f;
    public const float SpawnIntervalStart = 2.8f;
    public const float SpawnIntervalMin = 1.4f;
    public const float SpawnIntervalPerPipe = 0.045f;
    public const float MaxGapCenterDelta = 6.5f;
    public const float GapCenterLimit = 5.6f;

    // --- Economy ---
    public const int PipesPerStreakStep = 5;
    public const int MaxStreakBonus = 16;

    // --- Events / mini-games (frequent: first at pipe 4, then every 3-6 pipes) ---
    public const int FirstEventPipe = 4;
    public const int EventMinSpacing = 2;
    public const int EventMaxSpacing = 4;
    public const float BlockerWarningDist = 20f;
    public const float TurretCooldown = 0.5f;
    public const int BlockerHp = 3;
    public const float RandomMovingChance = 0.2f;    // chance a normal pipe oscillates

    // --- Biomes ---
    public const int BiomeSize = 10;                 // pipes per biome; every N passed → next biome

    // --- Bosses (spawn after 9 pipes of a biome, gate the biome change) ---
    public const float BossHoverX = 8f;              // boss parks this far right of center
    public const float BossOscAmp = 5.5f;
    public const float BossOscSpeed = 0.9f;
    public const float BossEnterSpeed = 5f;
    public const float BossFireInterval = 1.7f;
    public const float BossProjectileSpeed = 8.5f;
    public const int BossBaseHp = 8;
    public const int BossHpPerBiome = 4;
    public const int BossCoinReward = 20;
    public const int BossCoinPerBiome = 12;

    // --- Wind zones ---
    public const float WindForce = 38f;
    public const float WindZoneWidth = 12f;

    // --- Saw gates ---
    public const float SawOscSpeedMin = 1.6f;
    public const float SawOscSpeedMax = 2.6f;

    // --- Bird camera lock ---
    public const float BirdAnchorX = -4.5f;     // horizontal home in the camera view
    public const float BirdAnchorSpring = 3f;   // pull strength back to anchor
    public const float BirdAnchorMaxPull = 7f;

    // --- Coins ---
    public const float CoinValue = 1;
    public const float MagnetPullSpeed = 14f;

    // --- Camera / world ---
    public const float CameraOrthoSize = 13f;
    public const float GroundY = -11.5f;
    public const float CeilingY = 13f;

    // --- Shields / lives ---
    public const float ShieldInvincibleTime = 1.6f;
    public const float ReviveInvincibleTime = 2.5f;

    public static float PipeSpeed(int pipesPassed) =>
        Mathf.Min(MaxPipeSpeed, BasePipeSpeed + pipesPassed * SpeedPerPipe);

    public static float SpawnInterval(int pipesPassed) =>
        Mathf.Max(SpawnIntervalMin, SpawnIntervalStart - pipesPassed * SpawnIntervalPerPipe);

    public static float GapSize(int greaseLevel) => BaseGapSize + greaseLevel * 0.5f;
}
