using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("씬 이동 설정")]
    public string gameSceneName = "playe";
    public string startSceneName = "start";
    public string successSceneName = "success";
    public string failSceneName = "fail";

    [Header("UI Panels")]
    public GameObject titlePanel;     // ★ 시작화면 패널 (처음에 켜져있어야 함)
    public GameObject settingsPanel;
    public GameObject lobbyPanel;
    public GameObject shopPanel;
    public GameObject hudPanel;
    public GameObject resultPanel;

    [Header("인트로 컷신 패널")]
    public GameObject introPanel;     // ★ 만화 패널 (처음엔 꺼져있음)
    public Image introComicImage;

    [Header("UI Texts & Sliders")]
    public Text hudCoinText;
    public Text hudShieldText;
    public Text lobbySavedCoinText;
    public Text shopStatusText;
    public Text resultSummaryText;

    public Slider progressBar;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private bool isBgmOn = true;
    private bool isSfxOn = true;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // ★ 시작 씬(start)일 때의 초기 설정
        if (currentScene == startSceneName)
        {
            if (titlePanel != null) titlePanel.SetActive(true);   // 시작화면 켜기
            if (introPanel != null) introPanel.SetActive(false); // 만화 패널은 무조건 숨기기!
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);

        // BGM 재생
        if (AudioManager.Instance != null)
        {
            if (currentScene == startSceneName)
                AudioManager.Instance.PlayBGM(AudioManager.Instance.startBGM);
            else if (currentScene == gameSceneName)
                AudioManager.Instance.PlayBGM(AudioManager.Instance.playBGM);
            else if (currentScene == successSceneName || currentScene == failSceneName)
                AudioManager.Instance.PlayBGM(AudioManager.Instance.endingBGM);
        }

        if (currentScene == gameSceneName)
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
        }

        if (currentScene == successSceneName || currentScene == failSceneName)
        {
            int finalCoin = PlayerPrefs.GetInt("FinalCoin", 0);
            if (resultSummaryText != null) resultSummaryText.text = $"모은 코인수 : {finalCoin}";
        }
    }

    // ================== 버튼 클릭 함수 ==================
    public void OnClickGoToStartScreen()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    // ★ 시작 버튼을 눌렀을 때만 실행되는 로직
    public void OnClickStart()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (introPanel != null)
        {
            if (titlePanel != null) titlePanel.SetActive(false); // 시작화면 끄고!
            introPanel.SetActive(true);                          // 이제야 만화 화면 켜기!

            // 이미지 알파값(불투명도) 1로 고정
            if (introComicImage != null)
            {
                Color c = introComicImage.color;
                c.a = 1f;
                introComicImage.color = c;
            }

            // 3초 뒤 진짜 시작
            Invoke("OnClickRealStart", 3f);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnClickRealStart()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickSettings() { if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf); }
    public void OnClickCloseSettings() { if (settingsPanel != null) settingsPanel.SetActive(false); }
    public void OnClickQuit() { Application.Quit(); }

    public void UpdateHudUI(int coin, int shield) { if (hudCoinText != null) hudCoinText.text = $"코인: {coin}"; if (hudShieldText != null) hudShieldText.text = $"방어막: {shield}"; }
    public void UpdateProgressUI(int currentCoin, int maxCoin) { if (progressBar != null && maxCoin > 0) progressBar.value = Mathf.Clamp01((float)currentCoin / maxCoin); }
    public void ShowResultUI(string summary) { if (resultPanel != null) resultPanel.SetActive(true); if (resultSummaryText != null) resultSummaryText.text = summary; }
}