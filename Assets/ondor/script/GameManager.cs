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
        }
    }

    public void AddCoin(int amount = 1)
    {
        if (!isPlaying || isGameEnded) return;

        currentCoins += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
        }

        OnCoinUpdated?.Invoke(currentCoins, currentCoins);
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

    public void GameOver()
    {
        if (!isPlaying || isGameEnded) return;
        isPlaying = false;
        isGameEnded = true;

        // ★ 게임오버 SFX 및 BGM 전환 연동
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverSound();
            AudioManager.Instance.PlayBGM(AudioManager.Instance.endingBGM);
        }

        OnGameEndedTriggered?.Invoke(false, "Game Over", 0, 0, 0);

        PlayerPrefs.SetInt("FinalCoin", currentCoins);
        PlayerPrefs.Save();

        if (UIManager.Instance != null && UIManager.Instance.resultPanel != null)
        {
            Time.timeScale = 0f;
            UIManager.Instance.ShowResultUI($"획득 코인: {currentCoins}");
        }
        else
        {
            SceneManager.LoadScene("ending");
        }
    }

    // BossBattleManager 연동 더미 함수들
    public void SetCustomerSpawnEnabled(bool isEnabled, bool arg2)
    {
        if (IntegratedSpawner.Instance != null)
            IntegratedSpawner.Instance.isCustomerSpawnEnabled = isEnabled;
    }

    public void SetObstacleSpawnPaused(bool isPaused)
    {
        if (IntegratedSpawner.Instance != null)
            IntegratedSpawner.Instance.isObstacleSpawnPaused = isPaused;
    }

    public void DeductBossHit(int damage = 0) { }
    public void DeductBasicHit(int damage = 0) { }
    public void AddBossClearReward(int rewardAmount) { }
    public void CompleteAllBossChapters() { }
    public void ReserveLane(int laneIndex) { }
    public void ReleaseLane(int laneIndex) { }
    public void ClearAllReservedLanes() { }
    public void SetBossBattleDifficulty(int difficulty) { }
}