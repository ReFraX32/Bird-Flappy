using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Central game director: state machine (Menu → Playing → Dying → GameOver),
/// score/streak/coin economy, difficulty ramp, and bootstrap of every subsystem
/// (background, effects, UI, audio). Attached to the "Logic Manager" object.
/// </summary>
public class LogicScript : MonoBehaviour
{
    public static LogicScript Instance { get; private set; }

    public enum GameState { Menu, Playing, Dying, GameOver }
    public GameState State { get; private set; } = GameState.Menu;

    [SerializeField] private BirdScript bird; // kept for scene serialization
    public BirdScript Bird => bird;

    // run stats
    public int Score { get; private set; }
    public int PipesPassed { get; private set; }
    public int CoinsEarnedThisRun { get; private set; }
    public int ExtraLivesLeft { get; private set; }
    public int StreakStep => Mathf.Min(GameConfig.MaxStreakBonus, PipesPassed / GameConfig.PipesPerStreakStep);

    // difficulty
    public float CurrentSpeed => GameConfig.PipeSpeed(PipesPassed);
    public float CurrentGap => Mathf.Max(GameConfig.MinGapSize,
        GameConfig.BaseGapSize + Mathf.Min(6f, 0.5f * SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("grease")))
        - PipesPassed * 0.015f);
    public float CurrentSpawnInterval => GameConfig.SpawnInterval(PipesPassed);
    public float MagnetRadius
    {
        get
        {
            int lvl = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("magnet"));
            return lvl > 0 ? Mathf.Min(14f, 2f + 1.5f * lvl) : 0f;
        }
    }

    public ClosedPipeBlocker CurrentBlocker => ClosedPipeBlocker.FirstActive;
    public bool BossActive => BossScript.Active != null && !BossScript.Active.Dead;

    private int biomePipes;               // pipes passed within the current biome
    private bool bossDefeatedThisBiome;   // one boss per biome

    public bool IsPaused { get; private set; }
    public bool IsPlaying => State == GameState.Playing && !IsPaused;

    public bool IsPointerOverUI =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private PipeSpawnerScript spawner;
    private bool gameOverScheduled;

    void Awake()
    {
        Instance = this;
        Application.runInBackground = true;
        SaveSystem.Load();
        ProceduralAudio.Initialize(transform);

        // subsystem bootstraps
        var bgGo = new GameObject("Background");
        bgGo.AddComponent<BackgroundDesigner>();
        var fxGo = new GameObject("Effects");
        fxGo.AddComponent<EffectsManager>();

        spawner = FindAnyObjectByType<PipeSpawnerScript>();
        if (spawner == null)
        {
            var spGo = new GameObject("Pipe Spawner");
            spawner = spGo.AddComponent<PipeSpawnerScript>();
        }

        if (bird == null)
        {
            bird = FindAnyObjectByType<BirdScript>();
            if (bird == null)
                Debug.LogError("BirdScript not found in the scene. Please ensure the bird object is present.");
        }
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        GameUI.Build(this);
        EnterMenu();
    }

    // ------------------------------------------------------------ state flow

    public void EnterMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        State = GameState.Menu;
        Score = 0;
        PipesPassed = 0;
        CoinsEarnedThisRun = 0;
        biomePipes = 0;
        bossDefeatedThisBiome = false;
        if (BossScript.Active != null) Destroy(BossScript.Active.gameObject);
        ExtraLivesLeft = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("extraLife"));
        spawner.ResetForRun();
        if (bird != null) bird.EnterMenuMode();
        GameUI.Instance.ShowMenu();
        ProceduralAudio.PlayMusic();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        State = GameState.Playing;
        Score = 0;
        PipesPassed = 0;
        CoinsEarnedThisRun = 0;
        biomePipes = 0;
        bossDefeatedThisBiome = false;
        if (BossScript.Active != null) Destroy(BossScript.Active.gameObject);
        ExtraLivesLeft = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("extraLife"));
        spawner.ResetForRun();
        if (bird != null)
        {
            bird.RestoreForPlay();
            bird.BeginPlay();
        }
        GameUI.Instance.ShowHUD();
        int biome = BackgroundDesigner.Instance != null ? BackgroundDesigner.Instance.BiomeIndex : 0;
        ProceduralAudio.PlayMusic(biome);
        ProceduralAudio.PlaySfx("click", 0.8f);
    }

    public void RestartGame() => StartGame();

    public void BackToMenu() => EnterMenu();

    public void BirdCrashed()
    {
        if (State != GameState.Playing || bird == null) return;

        if (bird.Shields > 0)
        {
            bird.AbsorbHitWithShield();
            GameUI.Instance.UpdateShields(bird.Shields);
            return;
        }

        State = GameState.Dying;
        bird.OnDeath();
        EffectsManager.Instance.FeatherBurst(bird.transform.position);
        EffectsManager.Instance.Shake(0.45f);
        EffectsManager.Instance.FlashDeath();
        ProceduralAudio.PlaySfx("hit", 1f);
        Time.timeScale = 0.35f;

        if (!gameOverScheduled)
        {
            gameOverScheduled = true;
            StartCoroutine(GameOverSequence());
        }
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSecondsRealtime(1.15f);
        Time.timeScale = 1f;
        State = GameState.GameOver;
        gameOverScheduled = false;

        bool newBest = Score > SaveSystem.Data.bestScore;
        SaveSystem.RecordRun(Score);
        if (CoinsEarnedThisRun > 0) SaveSystem.AddCoins(CoinsEarnedThisRun);
        GameUI.Instance.ShowGameOver(Score, SaveSystem.Data.bestScore, CoinsEarnedThisRun, newBest, ExtraLivesLeft);
    }

    public bool CanRevive => State == GameState.GameOver && ExtraLivesLeft > 0;

    public void RequestRevive()
    {
        if (!CanRevive) return;
        ExtraLivesLeft--;
        State = GameState.Playing;
        Time.timeScale = 1f;

        // clear the danger zone around the bird
        spawner.ClearPipesNear(bird.transform.position, 14f);
        bird.Revive(new Vector3(bird.transform.position.x, Mathf.Clamp(bird.transform.position.y, -6f, 4f), 0f));
        GameUI.Instance.ShowHUD();
        ProceduralAudio.PlaySfx("revive", 1f);
    }

    public void TogglePause()
    {
        if (State != GameState.Playing) return;
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        GameUI.Instance.SetPauseVisible(IsPaused);
        ProceduralAudio.PlaySfx("click", 0.7f);
    }

    // ------------------------------------------------------------ gameplay events

    public void PipePassed(Vector3 at)
    {
        if (State != GameState.Playing) return;
        Score++;
        PipesPassed++;

        // boss gates each biome change: after 9 pipes, the biome's guardian arrives
        biomePipes++;
        if (!bossDefeatedThisBiome && biomePipes >= GameConfig.BiomeSize - 1 && !BossActive)
            StartBoss();

        // scaling payout: base grows with streak, golden beak multiplies
        float goldenMult = Mathf.Min(3f, 1f + 0.15f * SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("golden")));
        int payout = Mathf.Max(1, Mathf.RoundToInt((1 + StreakStep) * goldenMult));
        CoinsEarnedThisRun += payout;

        ProceduralAudio.PlaySfx("score", 0.8f);
        Color c = StreakStep > 0 ? new Color(1f, 0.78f, 0.25f) : Color.white;
        EffectsManager.Instance.FloatingText(at + Vector3.up * 1.2f, $"+{payout} ◉", c,
            StreakStep > 0 ? 46 : 38);
        GameUI.Instance.UpdateHUD(Score, CoinsEarnedThisRun, StreakStep);
    }

    public void CoinCollected(Vector3 at)
    {
        if (State != GameState.Playing) return;
        CoinsEarnedThisRun += 1;
        GameUI.Instance.UpdateHUD(Score, CoinsEarnedThisRun, StreakStep);
    }

    private void StartBoss()
    {
        int biome = BackgroundDesigner.Instance != null ? BackgroundDesigner.Instance.BiomeIndex : 0;
        BossScript.Spawn(biome);
        ProceduralAudio.PlaySfx("alarm", 1f);
    }

    public void AnnounceBoss(BossScript boss)
    {
        if (GameUI.Instance != null)
            GameUI.Instance.AnnounceBoss($"BIOME GUARDIAN  ·  HP {boss.MaxHp}");
    }

    public void BossDefeated(BossScript boss)
    {
        bossDefeatedThisBiome = false;   // ready for the next biome's guardian
        biomePipes = 0;

        int reward = GameConfig.BossCoinReward + GameConfig.BossCoinPerBiome *
                     (BackgroundDesigner.Instance != null ? BackgroundDesigner.Instance.BiomeIndex : 0);
        CoinsEarnedThisRun += reward;
        GameUI.Instance?.UpdateHUD(Score, CoinsEarnedThisRun, StreakStep);
        EffectsManager.Instance.FloatingText(boss.transform.position + Vector3.up * 3f,
            $"+{reward} ◉", UiTheme.Gold, 60);

        // biome transition unlocked
        if (BackgroundDesigner.Instance != null)
        {
            BackgroundDesigner.Instance.AdvanceBiome();
            var biome = BackgroundDesigner.Instance.Current;
            ProceduralAudio.PlaySfx("open", 0.9f);
            EffectsManager.Instance.FloatingText(boss.transform.position, biome.name, biome.accent, 54);
            GameUI.Instance?.AnnounceBiome(biome.name, biome.accent);
        }
    }

    public void NotifyFlap()
    {
        ProceduralAudio.PlaySfx("flap", 0.8f);
        EffectsManager.Instance.Burst(bird.transform.position + Vector3.down * 0.8f,
            new Color(1f, 1f, 1f, 0.8f), 3, 2.5f, 0.16f, 0.35f, 2f);
    }

    public void DespawnCoin(CoinScript coin) => spawner.DespawnCoin(coin);

    // legacy-context compatibility
    public void AddScore(int scoreToAdd) { if (State == GameState.Playing) Score += scoreToAdd; }
    public void GameOver() => BirdCrashed();
}
