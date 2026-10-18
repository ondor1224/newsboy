using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 진행 상태")]
    public bool isPlaying = false;
    public bool isGameEnded = false; // BossBattleManager 연동용
    public int currentCoins = 0;
    public int shieldCount = 0;

    [Header("참조 변수")]
    public player playerRef;

    // BossBattleManager가 구독(Subscribe)하는 이벤트 파라미터 매칭
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

        // 코인을 먹을 때마다 보스전 매니저에게 코인 획득 상황 전달
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

        // 게임 오버 시 이벤트 발생
        OnGameEndedTriggered?.Invoke(false, "Game Over", 0, 0, 0);

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

    // =========================================================
    // BossBattleManager에서 발생하는 에러 방지용 연동 함수들
    // =========================================================

    // =========================================================
    // BossBattleManager에서 호출하는 스포너 제어 함수
    // =========================================================

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

    // 나머지 더미 함수들
    public void DeductBossHit(int damage = 0) { }
    public void DeductBasicHit(int damage = 0) { }
    public void AddBossClearReward(int rewardAmount) { }
    public void CompleteAllBossChapters() { }
    public void ReserveLane(int laneIndex) { }
    public void ReleaseLane(int laneIndex) { }
    public void ClearAllReservedLanes() { }
    public void SetBossBattleDifficulty(int difficulty) { }
}