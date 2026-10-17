using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References & Spawner")]
    public player playerRef; // 소문자 player로 수정됨
    public Obstacle obstaclePrefab;
    public Customer leftCustomerPrefab, rightCustomerPrefab;
    public float spawnZ = 28f;
    public float[] laneX = { -2.5f, 0f, 2.5f };
    public float customerX = 4.5f;
    public float baseObstacleInterval = 2.2f;
    public float customerInterval = 2.8f;

    [Header("UI Panels")]
    public GameObject titlePanel;
    public GameObject settingsPanel;
    public GameObject lobbyPanel;
    public GameObject shopPanel;
    public GameObject hudPanel;
    public GameObject resultPanel;

    [Header("UI Texts & Sliders")]
    public Text hudCoinText;
    public Text hudShieldText;
    public Text lobbySavedCoinText;
    public Text shopStatusText;
    public Text resultSummaryText;
    public Slider bgmSlider, sfxSlider;

    [Header("Audio")]
    public AudioSource bgmSource, sfxSource;
    public AudioClip hitClip, coinClip, clickClip;

    [Header("Economy & Quota")]
    public int currentCoin;
    public int chapterProgressCoin;
    public int totalSavedCoin;
    public int basicHitPenalty = 10;
    public int bossHitPenalty = 15;
    public int dailyQuota = 1500;
    public int baseUpgradeCost = 100;

    [HideInInspector] public bool isPlaying;
    [HideInInspector] public bool isGameEnded;

    public int shieldLv, lifeLv, autoCustomerLv, doubleCoinLv;
    private int activeShields;
    private bool hasRevive;
    private float autoCustomerTimer;

    private bool customerSpawnEnabled = true;
    private bool obstacleSpawnPaused;
    private float obstacleTimer, customerTimer, currentObsInterval;
    private int bossChapterDifficulty;
    private readonly HashSet<int> reservedLanes = new HashSet<int>();
    private readonly List<Customer> activeCustomers = new List<Customer>();

    public bool IsAutoCustomerActive => autoCustomerTimer > 0f;
    public event Action<int, int> OnCoinUpdated;
    public event Action OnGameEndedTriggered;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        LoadData();
        currentObsInterval = baseObstacleInterval;
    }

    void Start()
    {
        Time.timeScale = 1f;
        if (bgmSlider) bgmSlider.value = PlayerPrefs.GetFloat("Ondor_BGM", 0.8f);
        if (sfxSlider) sfxSlider.value = PlayerPrefs.GetFloat("Ondor_SFX", 0.8f);
        ApplyVolume();
        ShowTitleScreen();
    }

    void Update()
    {
        if (!isPlaying || isGameEnded) return;

        if (autoCustomerTimer > 0f) autoCustomerTimer -= Time.deltaTime;

        if (!obstacleSpawnPaused && obstaclePrefab != null)
        {
            obstacleTimer += Time.deltaTime;
            if (obstacleTimer >= currentObsInterval)
            {
                obstacleTimer = 0f;
                SpawnObstacle();
            }
        }

        if (customerSpawnEnabled)
        {
            customerTimer += Time.deltaTime;
            if (customerTimer >= customerInterval)
            {
                customerTimer = 0f;
                SpawnCustomer();
            }
        }
    }

    public void ShowTitleScreen()
    {
        isPlaying = false;
        SetPanels(true, false, false, false, false, false);
    }

    public void OnClickOpenSettings(bool open)
    {
        PlaySFX(clickClip);
        if (settingsPanel) settingsPanel.SetActive(open);
    }

    public void ApplyVolume()
    {
        float b = bgmSlider ? bgmSlider.value : 0.8f;
        float s = sfxSlider ? sfxSlider.value : 0.8f;
        if (bgmSource) bgmSource.volume = b;
        if (sfxSource) sfxSource.volume = s;
        PlayerPrefs.SetFloat("Ondor_BGM", b);
        PlayerPrefs.SetFloat("Ondor_SFX", s);
    }

    public void OnClickProceedToLobby()
    {
        PlaySFX(clickClip);
        isPlaying = false;
        if (playerRef) playerRef.transform.position = Vector3.zero;
        SetPanels(false, false, true, false, false, false);
        RefreshShopUI();
    }

    public void OnClickToggleShop(bool open)
    {
        PlaySFX(clickClip);
        if (shopPanel) shopPanel.SetActive(open);
        RefreshShopUI();
    }

    public void OnClickStartGameRun()
    {
        PlaySFX(clickClip);
        currentCoin = 0;
        chapterProgressCoin = 0;
        isGameEnded = false;
        isPlaying = true;
        Time.timeScale = 1f;

        activeShields = shieldLv;
        hasRevive = lifeLv >= 1;
        RefillAutoCustomerBuff();

        SetPanels(false, false, false, false, true, false);
        UpdateHUD();
    }

    public void RefillAutoCustomerBuff()
    {
        if (autoCustomerLv > 0) autoCustomerTimer = autoCustomerLv * 8f;
    }

    public void BuyUpgrade(int type)
    {
        PlaySFX(clickClip);
        int lv = type == 0 ? shieldLv : (type == 1 ? lifeLv : (type == 2 ? autoCustomerLv : doubleCoinLv));
        int maxLv = type == 1 ? 1 : 5;
        if (lv >= maxLv) return;

        int cost = baseUpgradeCost * (lv + 1) * (type == 1 ? 3 : 1);
        if (totalSavedCoin < cost) return;

        totalSavedCoin -= cost;
        if (type == 0) shieldLv++;
        else if (type == 1) lifeLv = 1;
        else if (type == 2) autoCustomerLv++;
        else if (type == 3) doubleCoinLv++;

        SaveData();
        RefreshShopUI();
    }

    public void BuyShield() => BuyUpgrade(0);
    public void BuyLife() => BuyUpgrade(1);
    public void BuyAutoCustomer() => BuyUpgrade(2);
    public void BuyDoubleCoin() => BuyUpgrade(3);

    public void AddCustomerCoin(int baseAmount)
    {
        int mult = 1 + (doubleCoinLv >= 1 ? 1 : 0);
        int bonus = Mathf.Max(0, doubleCoinLv - 1) * 2;
        AddCoin(baseAmount * mult + bonus);
    }

    public void AddCoin(int amount)
    {
        if (isGameEnded || amount <= 0) return;
        currentCoin += amount;
        chapterProgressCoin += amount;
        PlaySFX(coinClip);
        UpdateHUD();
        OnCoinUpdated?.Invoke(currentCoin, chapterProgressCoin);
    }

    public void AddBossClearReward(int reward)
    {
        if (isGameEnded || reward <= 0) return;
        currentCoin += reward;
        chapterProgressCoin = 0;
        RefillAutoCustomerBuff();
        PlaySFX(coinClip);
        UpdateHUD();
        OnCoinUpdated?.Invoke(currentCoin, chapterProgressCoin);
    }

    public void DeductBasicHit() => DeductCoin(basicHitPenalty);
    public void DeductBossHit() => DeductCoin(bossHitPenalty);

    public void DeductCoin(int amount)
    {
        if (isGameEnded || amount <= 0) return;
        PlaySFX(hitClip);

        if (activeShields > 0)
        {
            activeShields--;
            UpdateHUD();
            return;
        }

        currentCoin = Mathf.Max(0, currentCoin - amount);
        UpdateHUD();

        if (currentCoin <= 0)
        {
            if (hasRevive)
            {
                hasRevive = false;
                lifeLv = 0;
                SaveData();
                currentCoin = 30;
                UpdateHUD();
                return;
            }
            EndGameSession(false);
        }
    }

    public void CompleteAllBossChapters() => EndGameSession(true);

    private void EndGameSession(bool cleared)
    {
        if (isGameEnded) return;
        isGameEnded = true;
        isPlaying = false;
        Time.timeScale = 0f;

        int raw = Mathf.Max(0, currentCoin);
        bool quotaFailed = raw < dailyQuota;
        int settled = quotaFailed ? Mathf.Max(0, raw - Mathf.RoundToInt(raw * 0.6f)) : raw;

        totalSavedCoin += settled;
        SaveData();

        string header = cleared ? "모든 보스전 클리어!" : "게임 오버";
        string quotaMsg = quotaFailed
            ? $"할당량 실패 (일일 목표 {dailyQuota}코인 미달 - 60% 차감)"
            : $"할당량 달성 성공! ({dailyQuota}코인 이상)";

        if (resultSummaryText)
            resultSummaryText.text = $"{header}\n{quotaMsg}\n획득 코인: {raw}\n정산 코인: {settled}\n총 누적 저장 코인: {totalSavedCoin}";

        SetPanels(false, false, false, false, false, true);
        OnGameEndedTriggered?.Invoke();
    }

    void SpawnObstacle()
    {
        List<int> free = new List<int>();
        for (int i = 0; i < laneX.Length; i++)
            if (!reservedLanes.Contains(i)) free.Add(i);
        if (free.Count <= 1) return;

        int lane = free[UnityEngine.Random.Range(0, free.Count)];
        Obstacle obs = Instantiate(obstaclePrefab, new Vector3(laneX[lane], 0f, spawnZ), Quaternion.identity);

        if (bossChapterDifficulty == 1) { obs.type = ObstacleType.Low; obs.speed = 5f; }
        else if (bossChapterDifficulty == 2) { obs.type = UnityEngine.Random.value < 0.5f ? ObstacleType.Low : ObstacleType.High; obs.speed = 6f; }
        else if (bossChapterDifficulty == 3)
        {
            ObstacleType[] all = { ObstacleType.Low, ObstacleType.High, ObstacleType.High_L, ObstacleType.High_R };
            obs.type = all[UnityEngine.Random.Range(0, all.Length)];
            obs.speed = 6.8f;
        }
        else { obs.type = UnityEngine.Random.value < 0.5f ? ObstacleType.Low : ObstacleType.High; obs.speed = 5.5f; }

        obs.SetupObstacleAsset();
    }

    void SpawnCustomer()
    {
        bool left = UnityEngine.Random.value < 0.5f;
        Customer prefab = left ? leftCustomerPrefab : rightCustomerPrefab;
        if (!prefab) return;
        Customer c = Instantiate(prefab, new Vector3(left ? -customerX : customerX, 0f, spawnZ), Quaternion.identity);
        c.side = left ? CustomerSide.Left : CustomerSide.Right;
        activeCustomers.Add(c);
    }

    public void SetCustomerSpawnEnabled(bool enabled, bool clearExisting)
    {
        customerSpawnEnabled = enabled;
        customerTimer = 0f;
        if (!clearExisting) return;
        for (int i = activeCustomers.Count - 1; i >= 0; i--)
        {
            if (activeCustomers[i]) { activeCustomers[i].isHandled = true; Destroy(activeCustomers[i].gameObject); }
        }
        activeCustomers.Clear();
    }

    public void SetBossBattleDifficulty(int chapter)
    {
        bossChapterDifficulty = chapter;
        currentObsInterval = chapter == 1 ? baseObstacleInterval * 1.6f :
                             chapter == 2 ? baseObstacleInterval * 1.3f :
                             chapter == 3 ? baseObstacleInterval * 1.15f : baseObstacleInterval;
    }

    public void SetObstacleSpawnPaused(bool paused) => obstacleSpawnPaused = paused;
    public void ReserveLane(int lane) => reservedLanes.Add(lane);
    public void ReleaseLane(int lane) => reservedLanes.Remove(lane);
    public void ClearAllReservedLanes() => reservedLanes.Clear();

    void SetPanels(bool title, bool settings, bool lobby, bool shop, bool hud, bool result)
    {
        if (titlePanel) titlePanel.SetActive(title);
        if (settingsPanel) settingsPanel.SetActive(settings);
        if (lobbyPanel) lobbyPanel.SetActive(lobby);
        if (shopPanel) shopPanel.SetActive(shop);
        if (hudPanel) hudPanel.SetActive(hud);
        if (resultPanel) resultPanel.SetActive(result);
    }

    void UpdateHUD()
    {
        if (hudCoinText) hudCoinText.text = $"COIN: {currentCoin}";
        if (hudShieldText) hudShieldText.text = $"SHIELD: {activeShields} | LIFE: {(hasRevive ? 1 : 0)}";
    }

    void RefreshShopUI()
    {
        if (lobbySavedCoinText) lobbySavedCoinText.text = $"보유 누적 코인: {totalSavedCoin}";
        if (shopStatusText)
            shopStatusText.text = $"방패 Lv.{shieldLv}/5 | 목숨 Lv.{lifeLv}/1\n자동접대 Lv.{autoCustomerLv}/5 | 코인2배 Lv.{doubleCoinLv}/5";
    }

    void PlaySFX(AudioClip clip)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip);
    }

    void LoadData()
    {
        totalSavedCoin = PlayerPrefs.GetInt("Ondor_TotalSavedCoin", 0);
        shieldLv = PlayerPrefs.GetInt("Ondor_ShieldLv", 0);
        lifeLv = PlayerPrefs.GetInt("Ondor_LifeLv", 0);
        autoCustomerLv = PlayerPrefs.GetInt("Ondor_AutoLv", 0);
        doubleCoinLv = PlayerPrefs.GetInt("Ondor_DoubleLv", 0);
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("Ondor_TotalSavedCoin", totalSavedCoin);
        PlayerPrefs.SetInt("Ondor_ShieldLv", shieldLv);
        PlayerPrefs.SetInt("Ondor_LifeLv", lifeLv);
        PlayerPrefs.SetInt("Ondor_AutoLv", autoCustomerLv);
        PlayerPrefs.SetInt("Ondor_DoubleLv", doubleCoinLv);
        PlayerPrefs.Save();
    }
}