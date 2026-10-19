using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 진행 상태")]
    public bool isPlaying = false;
    public bool isGameEnded = false;
    public int currentCoins = 0;
    public int shieldCount = 0;

    [Header("목적지 (진행도) 설정")]
    public int targetCoins = 500; // 목적지 도착을 위한 목표 코인 수

    [Header("엔딩 씬 이름 설정")]
    public string successSceneName = "success"; // 성공 시 넘어갈 씬 이름
    public string failSceneName = "fail";       // 실패 시 넘어갈 씬 이름

    [Header("참조 변수")]
    public player playerRef;

    public event Action<int, int> OnCoinUpdated;
    public event Action<bool, string, int, int, int> OnGameEndedTriggered;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        isPlaying = true;
        isGameEnded = false;
        Time.timeScale = 1f;
        currentCoins = 0;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
            UIManager.Instance.UpdateProgressUI(currentCoins, targetCoins);
        }
    }

    public void AddCoin(int amount = 1)
    {
        if (!isPlaying || isGameEnded) return;

        currentCoins += amount;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
            UIManager.Instance.UpdateProgressUI(currentCoins, targetCoins);
        }

        OnCoinUpdated?.Invoke(currentCoins, currentCoins);

        // ★ 500개 꽉 채우면 성공 처리
        if (currentCoins >= targetCoins)
        {
            GameClear();
        }
    }

    public void AddShield(int amount = 1)
    {
        if (!isPlaying || isGameEnded) return;

        shieldCount += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
        }
    }

    public bool TryUseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
            }
            return true;
        }

        GameOver();
        return false;
    }

    // ★ 실패 엔딩 (장애물 충돌)
    public void GameOver()
    {
        if (!isPlaying || isGameEnded) return;
        isPlaying = false;
        isGameEnded = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverSound();
            AudioManager.Instance.PlayBGM(AudioManager.Instance.endingBGM);
        }

        PlayerPrefs.SetInt("FinalCoin", currentCoins);
        PlayerPrefs.SetInt("IsClear", 0);
        PlayerPrefs.Save();

        if (UIManager.Instance != null && UIManager.Instance.resultPanel != null)
        {
            Time.timeScale = 0f;
            UIManager.Instance.ShowResultUI($"모은 코인수 : {currentCoins}");
        }
        else
        {
            // ★ 실패 씬으로 이동
            SceneManager.LoadScene(failSceneName);
        }
    }

    // ★ 성공 엔딩 (목적지 도착)
    public void GameClear()
    {
        if (!isPlaying || isGameEnded) return;
        isPlaying = false;
        isGameEnded = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameClearSound();
            AudioManager.Instance.PlayBGM(AudioManager.Instance.endingBGM);
        }

        PlayerPrefs.SetInt("FinalCoin", currentCoins);
        PlayerPrefs.SetInt("IsClear", 1);
        PlayerPrefs.Save();

        if (UIManager.Instance != null && UIManager.Instance.resultPanel != null)
        {
            Time.timeScale = 0f;
            UIManager.Instance.ShowResultUI($"모은 코인수 : {currentCoins}");
        }
        else
        {
            // ★ 성공 씬으로 이동
            SceneManager.LoadScene(successSceneName);
        }
    }

    // 기타 연동 함수들
    public void SetCustomerSpawnEnabled(bool isEnabled, bool arg2) { if (IntegratedSpawner.Instance != null) IntegratedSpawner.Instance.isCustomerSpawnEnabled = isEnabled; }
    public void SetObstacleSpawnPaused(bool isPaused) { if (IntegratedSpawner.Instance != null) IntegratedSpawner.Instance.isObstacleSpawnPaused = isPaused; }
    public void DeductBossHit(int damage = 0) { }
    public void DeductBasicHit(int damage = 0) { }
    public void AddBossClearReward(int rewardAmount) { }
    public void CompleteAllBossChapters() { }
    public void ReserveLane(int laneIndex) { }
    public void ReleaseLane(int laneIndex) { }
    public void ClearAllReservedLanes() { }
    public void SetBossBattleDifficulty(int difficulty) { }
}