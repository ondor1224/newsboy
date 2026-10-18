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
    public string endingSceneName = "ending";

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
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        string currentScene = SceneManager.GetActiveScene().name;

        // ★ 씬 시작 시 자동 BGM 재생
        if (AudioManager.Instance != null)
        {
            if (currentScene == startSceneName)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.startBGM);
            }
            else if (currentScene == gameSceneName)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.playBGM);
            }
            else if (currentScene == endingSceneName)
            {
                AudioManager.Instance.PlayBGM(AudioManager.Instance.endingBGM);
            }
        }

        if (currentScene == gameSceneName)
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
        }

        if (currentScene == endingSceneName)
        {
            int finalCoin = PlayerPrefs.GetInt("FinalCoin", 0);
            if (resultSummaryText != null)
            {
                resultSummaryText.text = $"모은 코인수 : {finalCoin}";
            }
        }
    }

    // ==================== 버튼 기능 & 효과음 ====================

    public void OnClickGoToStartScreen()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    public void OnClickStart()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickSettings()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnClickCloseSettings()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnClickQuit()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================== 배경음 / 효과음 토글 ====================

    public void OnClickToggleBgm()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        isBgmOn = !isBgmOn;
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMActive(isBgmOn);

        string targetText = isBgmOn ? "배경음 킴" : "배경음 끔";
        ChangeButtonTextOrChild(targetText, isBgmOn, "Music_on", "Music_off");
    }

    public void OnClickToggleSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        isSfxOn = !isSfxOn;
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXActive(isSfxOn);

        string targetText = isSfxOn ? "효과음 킴" : "효과음 끔";
        ChangeButtonTextOrChild(targetText, isSfxOn, "Musec_j_on", "Musec_j_off");
    }

    private void ChangeButtonTextOrChild(string newText, bool isOn, string onObjName, string offObjName)
    {
        GameObject clickedBtn = EventSystem.current.currentSelectedGameObject;
        if (clickedBtn == null) return;

        Text uiText = clickedBtn.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            uiText.text = newText;
        }

        Component[] allComponents = clickedBtn.GetComponentsInChildren<Component>(true);
        foreach (Component comp in allComponents)
        {
            if (comp != null && comp.GetType().Name.Contains("TextMeshPro"))
            {
                var textProp = comp.GetType().GetProperty("text");
                if (textProp != null) textProp.SetValue(comp, newText, null);
            }
        }

        Transform onChild = clickedBtn.transform.Find(onObjName);
        Transform offChild = clickedBtn.transform.Find(offObjName);
        if (onChild != null && offChild != null)
        {
            onChild.gameObject.SetActive(isOn);
            offChild.gameObject.SetActive(!isOn);
        }
    }

    // ==================== HUD / 결과 UI 업데이트 ====================

    public void UpdateHudUI(int coin, int shield)
    {
        if (hudCoinText != null) hudCoinText.text = $"코인: {coin}";
        if (hudShieldText != null) hudShieldText.text = $"방어막: {shield}";
    }

    public void ShowResultUI(string summary)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultSummaryText != null) resultSummaryText.text = summary;
    }
}