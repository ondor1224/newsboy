using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 진행 상태")]
    public bool isPlaying = false;
    public int currentCoins = 0;
    public int shieldCount = 0;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // 멈춘 시간 강제 해제
    }

    private void Start()
    {
        // playe 씬으로 넘어오면 대기하지 않고 즉시 게임 시작!
        StartGame();
    }

    public void StartGame()
    {
        isPlaying = true;
        Time.timeScale = 1f;
        currentCoins = 0;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
        }

        Debug.Log("게임 플레이가 시작되었습니다!");
    }

    public void AddCoin(int amount = 1)
    {
        if (!isPlaying) return;

        currentCoins += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
        }
    }

    public void AddShield(int amount = 1)
    {
        if (!isPlaying) return;

        shieldCount += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
        }
    }

    // 장애물 충돌 시 호출
    public bool TryUseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHudUI(currentCoins, shieldCount);
            }
            return true; // 방어 성공
        }

        GameOver();
        return false; // 방어 실패 -> 게임 오버
    }

    public void GameOver()
    {
        if (!isPlaying) return;
        isPlaying = false;

        Debug.Log("게임 오버!");
        if (UIManager.Instance != null && UIManager.Instance.resultPanel != null)
        {
            Time.timeScale = 0f;
            UIManager.Instance.ShowResultUI($"획득 코인: {currentCoins}");
        }
        else
        {
            // 결과 패널이 없으면 ending 씬으로 이동
            SceneManager.LoadScene("ending");
        }
    }
}