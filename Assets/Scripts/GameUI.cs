using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built UGUI: HUD (score / run coins / streak / shields / pause),
/// main menu with shop access, upgrade shop, pause and game over panels.
/// Fully responsive (scale-with-screen-size) for PC and mobile.
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private LogicScript logic;

    private Canvas canvas;
    private RectTransform hud, menuPanel, shopPanel, gameOverPanel, pausePanel;

    private Text hudScore, hudCoins, menuCoins, shopBalance;
    private GameObject streakPill, newBestFlair, reviveBtn;
    private Text gameOverScore, gameOverBest, gameOverCoins, gameOverStreak;
    private Text bestScoreLabel;
    private readonly List<Image> shieldIcons = new List<Image>();
    private readonly List<GameObject> shopCards = new List<GameObject>();
    private RectTransform shopGrid;
    private Text soundToggleLabel;

    public static void Build(LogicScript owner)
    {
        if (Instance != null) { Destroy(Instance.gameObject); Instance = null; }
        var go = new GameObject("GameUI", typeof(GameUI));
        Instance = go.GetComponent<GameUI>();
        Instance.logic = owner;
        Instance.BuildAll();
    }

    private void BuildAll()
    {
        var canvasGo = new GameObject("GameUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 50f;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = Screen.height > Screen.width ? 1f : 0.5f; // portrait phones match height

        hud = BuildHud();
        menuPanel = BuildMenu();
        shopPanel = BuildShop();
        gameOverPanel = BuildGameOver();
        pausePanel = BuildPause();

        UiTheme.FinalizePanelImages(canvas);
        Canvas.ForceUpdateCanvases();

    }

    // ================================================================ HUD

    private RectTransform BuildHud()
    {
        var root = new GameObject("HUD", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        UiTheme.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<SafeAreaFitter>();

        hudScore = UiTheme.CreateOutlinedText(root, "Score", "0", 110, Color.white);
        UiTheme.Place((RectTransform)hudScore.transform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(500, 130));

        // coin pill
        var pill = UiTheme.CreatePanel(root, "CoinPill", "uiPanelDark", UiTheme.Navy);
        UiTheme.Place(pill, new Vector2(0f, 1f), new Vector2(40, -45), new Vector2(280, 86));
        var coinIcon = UiTheme.CreateImage(pill, "Icon", "coin", Color.white);
        UiTheme.Place((RectTransform)coinIcon.transform, new Vector2(0f, 0.5f), new Vector2(44, 0), new Vector2(52, 52));
        hudCoins = UiTheme.CreateText(pill, "Coins", "+0", 46, UiTheme.Gold, TextAnchor.MiddleLeft);
        UiTheme.Place((RectTransform)hudCoins.transform, new Vector2(0f, 0.5f), new Vector2(110, 0), new Vector2(160, 60));

        // streak pill
        var streak = UiTheme.CreatePanel(root, "StreakPill", "uiPanelDark", new Color(0.98f, 0.62f, 0.13f));
        UiTheme.Place((RectTransform)streak.transform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(240, 66));
        var streakText = UiTheme.CreateText(streak, "Streak", "STREAK x2", 34, Color.white);
        streakText.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 66);
        streakPill = streak.gameObject;
        streakPill.SetActive(false);

        // shields row
        for (int i = 0; i < 5; i++)
        {
            var img = UiTheme.CreateImage(root, $"Shield{i}", "iconShield", Color.white);
            UiTheme.Place((RectTransform)img.transform, new Vector2(0f, 1f), new Vector2(60 + i * 66, -140), new Vector2(52, 52));
            shieldIcons.Add(img);
            img.gameObject.SetActive(false);
        }

        // pause button
        var pauseBtn = UiTheme.CreateButton(root, "PauseBtn", "II", "uiCircle", 40, Color.white, () => logic.TogglePause());
        UiTheme.Place((RectTransform)pauseBtn.transform, new Vector2(1f, 1f), new Vector2(-60, -60), new Vector2(88, 88));
        pauseBtn.image.color = new Color(0.2f, 0.22f, 0.35f, 0.85f);

        return root;
    }

    // ================================================================ MENU

    private RectTransform BuildMenu()
    {
        var root = new GameObject("Menu", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        UiTheme.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var title = UiTheme.CreateOutlinedText(root, "Title", "FLAPPY QUEST", 130, Color.white);
        UiTheme.Place((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1400, 170));

        var play = UiTheme.CreateButton(root, "PlayBtn", "PLAY", "uiButtonGreen", 64, Color.white,
            () => logic.StartGame());
        UiTheme.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(360, 120));

        var shop = UiTheme.CreateButton(root, "ShopBtn", "UPGRADES", "uiButtonAmber", 44, Color.white,
            () => { OpenShop(); });
        UiTheme.Place((RectTransform)shop.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(320, 92));

        bestScoreLabel = UiTheme.CreateOutlinedText(root, "Best", "", 40, Color.white);
        UiTheme.Place((RectTransform)bestScoreLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(800, 60));

        // wallet pill (top-left)
        var pill = UiTheme.CreatePanel(root, "WalletPill", "uiPanelDark", UiTheme.Navy);
        UiTheme.Place(pill, new Vector2(0f, 1f), new Vector2(40, -45), new Vector2(280, 86));
        var coinIcon = UiTheme.CreateImage(pill, "Icon", "coin", Color.white);
        UiTheme.Place((RectTransform)coinIcon.transform, new Vector2(0f, 0.5f), new Vector2(44, 0), new Vector2(52, 52));
        menuCoins = UiTheme.CreateText(pill, "Coins", "0", 46, UiTheme.Gold, TextAnchor.MiddleLeft);
        UiTheme.Place((RectTransform)menuCoins.transform, new Vector2(0f, 0.5f), new Vector2(110, 0), new Vector2(160, 60));

        // sound toggle (top-right)
        var soundBtn = UiTheme.CreateButton(root, "SoundBtn", "♪ ON", "uiCircle", 26, Color.white, ToggleSound);
        UiTheme.Place((RectTransform)soundBtn.transform, new Vector2(1f, 1f), new Vector2(-70, -60), new Vector2(120, 120));
        soundBtn.image.color = new Color(0.2f, 0.22f, 0.35f, 0.85f);
        soundToggleLabel = soundBtn.GetComponentInChildren<Text>();

        return root;
    }

    private void ToggleSound()
    {
        ProceduralAudio.ToggleSound();
        RefreshSoundLabel();
        ProceduralAudio.PlaySfx("click", 0.7f);
    }

    private void RefreshSoundLabel()
    {
        if (soundToggleLabel != null)
            soundToggleLabel.text = SaveSystem.Data.soundEnabled ? "♪ ON" : "♪ OFF";
    }

    // ================================================================ SHOP

    private RectTransform BuildShop()
    {
        var root = new GameObject("Shop", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        UiTheme.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backdrop = UiTheme.CreateImage(root, "Backdrop", "white", new Color(0.05f, 0.07f, 0.12f, 0.85f));
        UiTheme.Stretch((RectTransform)backdrop.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var panel = UiTheme.CreatePanel(root, "Panel", "uiPanel", Color.white);
        UiTheme.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 940));
        panel.gameObject.name = "ShopPanel";

        var header = UiTheme.CreateText(panel, "Header", "UPGRADE SHOP", 64, UiTheme.Navy);
        UiTheme.Place((RectTransform)header.transform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(600, 90));

        // balance pill
        var balPill = UiTheme.CreatePanel(panel, "Balance", "uiPanelDark", UiTheme.Navy);
        UiTheme.Place(balPill, new Vector2(0.5f, 1f), new Vector2(0, -165), new Vector2(260, 76));
        var balIcon = UiTheme.CreateImage(balPill, "Icon", "coin", Color.white);
        UiTheme.Place((RectTransform)balIcon.transform, new Vector2(0f, 0.5f), new Vector2(44, 0), new Vector2(48, 48));
        shopBalance = UiTheme.CreateText(balPill, "Coins", "0", 42, UiTheme.Gold);
        UiTheme.Place((RectTransform)shopBalance.transform, new Vector2(0f, 0.5f), new Vector2(140, 0), new Vector2(160, 60));

        // scroll view
        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D), typeof(Image));
        scrollGo.transform.SetParent(panel, false);
        var scrollRt = (RectTransform)scrollGo.transform;
        UiTheme.Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(920, 620));
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30;

        var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
        viewport.SetParent(scrollRt, false);
        UiTheme.Stretch(viewport, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        UiTheme.Stretch(content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(450, 260);
        grid.spacing = new Vector2(20, 20);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.childAlignment = TextAnchor.UpperCenter;
        shopGrid = content;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var close = UiTheme.CreateButton(panel, "CloseBtn", "✕", "uiButtonRed", 44, Color.white, CloseShop);
        UiTheme.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-70, -70), new Vector2(88, 88));

        return root;
    }

    public void OpenShop()
    {
        menuPanel.gameObject.SetActive(false);
        shopPanel.gameObject.SetActive(true);
        RebuildShopCards();
        ProceduralAudio.PlaySfx("click", 0.8f);
    }

    public void CloseShop()
    {
        shopPanel.gameObject.SetActive(false);
        menuPanel.gameObject.SetActive(true);
        RefreshMenuStats();
    }

    private void RebuildShopCards()
    {
        foreach (var card in shopCards)
            if (card != null) Destroy(card);
        shopCards.Clear();
        shopBalance.text = SaveSystem.Data.coins.ToString();

        for (int i = 0; i < UpgradeCatalog.All.Length; i++)
        {
            var def = UpgradeCatalog.All[i];
            int level = SaveSystem.GetUpgradeLevel(i);
            var card = UiTheme.CreatePanel(shopGrid, $"Card_{def.id}", "uiPanel", Color.white);
            shopCards.Add(card.gameObject);

            // left icon column
            var icon = UiTheme.CreateImage(card, "Icon", $"icon{def.icon}", Color.white);
            UiTheme.Place((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(58, -6), new Vector2(84, 84));

            // name — anchored top-left of the text area, BestFit so long names never clip
            var nameT = UiTheme.CreateText(card, "Name", def.name, 30, UiTheme.Navy, TextAnchor.LowerLeft);
            UiTheme.Place((RectTransform)nameT.transform, new Vector2(0f, 1f), new Vector2(118, -14), new Vector2(312, 46));
            nameT.resizeTextForBestFit = true;
            nameT.resizeTextMinSize = 15;
            nameT.resizeTextMaxSize = 30;

            // description — wrapped and shrunk to fit a fixed safe box
            var desc = UiTheme.CreateText(card, "Desc", def.description, 20, new Color(0.35f, 0.38f, 0.47f), TextAnchor.UpperLeft);
            var descRt = (RectTransform)desc.transform;
            UiTheme.Place(descRt, new Vector2(0f, 1f), new Vector2(118, -64), new Vector2(312, 118));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Truncate;
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 12;
            desc.resizeTextMaxSize = 20;

            // current level label (upgrades are infinite)
            var lvlLabel = UiTheme.CreateText(card, "Level", $"Lv {level}", 26, UiTheme.Green, TextAnchor.LowerLeft);
            UiTheme.Place((RectTransform)lvlLabel.transform, new Vector2(0f, 0f), new Vector2(118, 12), new Vector2(130, 38));

            // buy button — bottom-right, generous touch target
            int cost = def.CostForLevel(level);
            var buy = UiTheme.CreateButton(card, "Buy", $"◉ {cost}", "uiButtonGreen", 28, Color.white, null);
            UiTheme.Place((RectTransform)buy.transform, new Vector2(1f, 0f), new Vector2(-18, 12), new Vector2(156, 66));
            int index = i;
            buy.interactable = SaveSystem.Data.coins >= cost;
            if (buy.interactable)
                buy.onClick.AddListener(() => BuyUpgrade(index));
        }
    }

    private void BuyUpgrade(int index)
    {
        var def = UpgradeCatalog.All[index];
        int level = SaveSystem.GetUpgradeLevel(index);
        int cost = def.CostForLevel(level);
        if (!SaveSystem.TrySpend(cost)) return;
        SaveSystem.SetUpgradeLevel(index, level + 1);
        ProceduralAudio.PlaySfx("buy", 1f);
        RebuildShopCards();
    }

    // ================================================================ GAME OVER

    private RectTransform BuildGameOver()
    {
        var root = new GameObject("GameOver", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        UiTheme.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backdrop = UiTheme.CreateImage(root, "Backdrop", "white", new Color(0.05f, 0.07f, 0.12f, 0.8f));
        UiTheme.Stretch((RectTransform)backdrop.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var panel = UiTheme.CreatePanel(root, "Panel", "uiPanel", Color.white);
        UiTheme.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 780));

        var title = UiTheme.CreateText(panel, "Title", "GAME OVER", 84, UiTheme.Red);
        UiTheme.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(600, 110));

        newBestFlair = UiTheme.CreateText(panel, "NewBest", "★ NEW BEST! ★", 38, UiTheme.Amber).gameObject;
        UiTheme.Place((RectTransform)newBestFlair.transform, new Vector2(0.5f, 1f), new Vector2(0, -165), new Vector2(500, 50));

        gameOverScore = UiTheme.CreateOutlinedText(panel, "ScoreVal", "0", 110, UiTheme.Navy);
        UiTheme.Place((RectTransform)gameOverScore.transform, new Vector2(0.5f, 1f), new Vector2(0, -250), new Vector2(500, 150));

        gameOverBest = UiTheme.CreateText(panel, "BestVal", "", 34, new Color(0.35f, 0.38f, 0.47f));
        UiTheme.Place((RectTransform)gameOverBest.transform, new Vector2(0.5f, 1f), new Vector2(0, -412), new Vector2(600, 44));

        gameOverStreak = UiTheme.CreateText(panel, "StreakVal", "", 30, new Color(0.35f, 0.38f, 0.47f));
        UiTheme.Place((RectTransform)gameOverStreak.transform, new Vector2(0.5f, 1f), new Vector2(0, -458), new Vector2(600, 40));

        // coins earned pill
        var coinRow = UiTheme.CreatePanel(panel, "CoinsRow", "uiPanelDark", UiTheme.Navy);
        UiTheme.Place(coinRow, new Vector2(0.5f, 1f), new Vector2(0, -520), new Vector2(340, 84));
        var coinIcon = UiTheme.CreateImage(coinRow, "Icon", "coin", Color.white);
        UiTheme.Place((RectTransform)coinIcon.transform, new Vector2(0f, 0.5f), new Vector2(52, 0), new Vector2(56, 56));
        gameOverCoins = UiTheme.CreateText(coinRow, "Coins", "+0", 44, UiTheme.Gold);
        UiTheme.Place((RectTransform)gameOverCoins.transform, new Vector2(0f, 0.5f), new Vector2(170, 0), new Vector2(200, 60));

        var revive = UiTheme.CreateButton(panel, "ReviveBtn", "PHOENIX REVIVE", "uiButtonGreen", 36, Color.white,
            () => logic.RequestRevive());
        UiTheme.Place((RectTransform)revive.transform, new Vector2(0.5f, 0f), new Vector2(0, 165), new Vector2(420, 88));
        reviveBtn = revive.gameObject;

        var restart = UiTheme.CreateButton(panel, "RestartBtn", "PLAY AGAIN", "uiButtonAmber", 44, Color.white,
            () => logic.RestartGame());
        UiTheme.Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(-190, 55), new Vector2(280, 96));

        var menu = UiTheme.CreateButton(panel, "MenuBtn", "MENU", "uiButtonGrey", 44, Color.white,
            () => logic.BackToMenu());
        UiTheme.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(190, 55), new Vector2(280, 96));

        return root;
    }

    // ================================================================ PAUSE

    private RectTransform BuildPause()
    {
        var root = new GameObject("Pause", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        UiTheme.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backdrop = UiTheme.CreateImage(root, "Backdrop", "white", new Color(0.05f, 0.07f, 0.12f, 0.7f));
        UiTheme.Stretch((RectTransform)backdrop.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var panel = UiTheme.CreatePanel(root, "Panel", "uiPanel", Color.white);
        UiTheme.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 440));

        var title = UiTheme.CreateText(panel, "Title", "PAUSED", 64, UiTheme.Navy);
        UiTheme.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(400, 90));

        var resume = UiTheme.CreateButton(panel, "ResumeBtn", "RESUME", "uiButtonGreen", 44, Color.white,
            () => logic.TogglePause());
        UiTheme.Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 92));

        var menu = UiTheme.CreateButton(panel, "MenuBtn", "QUIT TO MENU", "uiButtonGrey", 40, Color.white,
            () => logic.BackToMenu());
        UiTheme.Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(320, 92));

        return root;
    }

    // ================================================================ visibility & updates

    public void ShowMenu()
    {
        hud.gameObject.SetActive(false);
        menuPanel.gameObject.SetActive(true);
        shopPanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        RefreshMenuStats();
        RefreshSoundLabel();
    }

    public void ShowHUD()
    {
        hud.gameObject.SetActive(true);
        menuPanel.gameObject.SetActive(false);
        shopPanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        UpdateHUD(0, 0, 0);
        UpdateShields(logic.Bird != null ? logic.Bird.Shields : 0);
    }

    public void ShowGameOver(int score, int best, int coins, bool newBest, int livesLeft)
    {
        hud.gameObject.SetActive(false);
        menuPanel.gameObject.SetActive(false);
        gameOverPanel.gameObject.SetActive(true);
        gameOverScore.text = score.ToString();
        gameOverBest.text = $"BEST: {best}";
        gameOverCoins.text = $"+{coins}";
        gameOverStreak.text = $"top streak bonus x{(1 + logic.StreakStep)}";
        newBestFlair.SetActive(newBest);
        reviveBtn.SetActive(livesLeft > 0);
        if (newBest)
            Tween.Scale(newBestFlair.transform, Vector3.one * 0.4f, Vector3.one, 0.5f, Tween.EaseOutBack);
    }

    public void SetPauseVisible(bool visible)
    {
        pausePanel.gameObject.SetActive(visible);
        hud.gameObject.SetActive(!visible || logic.State == LogicScript.GameState.Playing);
    }

    public void UpdateHUD(int score, int runCoins, int streakStep)
    {
        hudScore.text = score.ToString();
        hudCoins.text = $"+{runCoins}";
        bool streak = streakStep > 0;
        streakPill.SetActive(streak);
        if (streak)
            streakPill.transform.GetChild(0).GetComponent<Text>().text = $"STREAK x{1 + streakStep}";
    }

    private Text biomeToast;
    private float biomeToastUntil;

    public void AnnounceBoss(string info)
    {
        EnsureToast();
        biomeToast.text = info;
        biomeToast.color = new Color(0.95f, 0.35f, 0.35f);
        biomeToastUntil = Time.unscaledTime + 2.8f;
    }

    private void EnsureToast()
    {
        if (biomeToast != null) return;
        biomeToast = UiTheme.CreateOutlinedText(canvas.transform, "BiomeToast", "", 60, Color.white);
        var rt = (RectTransform)biomeToast.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 210);
        rt.sizeDelta = new Vector2(1700, 100);
        biomeToast.resizeTextForBestFit = true;
        biomeToast.resizeTextMinSize = 30;
        biomeToast.resizeTextMaxSize = 60;
    }

    public void AnnounceBiome(string name, Color color)
    {
        EnsureToast();
        biomeToast.text = "—  " + name + "  —";
        biomeToast.color = color;
        biomeToastUntil = Time.unscaledTime + 2.4f;
        ((RectTransform)biomeToast.transform).localScale = Vector3.one * 0.6f;
        Tween.Scale(biomeToast.transform, Vector3.one * 0.6f, Vector3.one, 0.45f, Tween.EaseOutBack);
    }

    public void UpdateShields(int count)
    {
        for (int i = 0; i < shieldIcons.Count; i++)
            shieldIcons[i].gameObject.SetActive(i < count);
    }

    private void RefreshMenuStats()
    {
        menuCoins.text = SaveSystem.Data.coins.ToString();
        bestScoreLabel.text = SaveSystem.Data.bestScore > 0 ? $"BEST  {SaveSystem.Data.bestScore}" : "";
    }

    private int lastScreenWidth, lastScreenHeight;

    private void Update()
    {
        // keep the canvas matched to the current orientation (rotation on mobile)
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            var sc = canvas.GetComponent<CanvasScaler>();
            if (sc != null) sc.matchWidthOrHeight = Screen.height > Screen.width ? 1f : 0.5f;
        }

        // biome toast lifecycle
        if (biomeToast != null && biomeToastUntil > 0f)
        {
            if (Time.unscaledTime > biomeToastUntil)
            {
                var c = biomeToast.color; c.a = Mathf.Max(0f, c.a - Time.unscaledDeltaTime * 1.5f);
                biomeToast.color = c;
                if (c.a <= 0f) { biomeToastUntil = 0f; biomeToast.text = ""; biomeToast.color = new Color(c.r, c.g, c.b, 1f); }
            }
        }

        // keyboard shortcuts (PC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (logic.State == LogicScript.GameState.Playing) logic.TogglePause();
        }
        if (Input.GetKeyDown(KeyCode.R) && logic.State == LogicScript.GameState.GameOver)
            logic.RestartGame();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (logic.State == LogicScript.GameState.Menu && !shopPanel.gameObject.activeSelf)
                logic.StartGame();
        }
    }
}
