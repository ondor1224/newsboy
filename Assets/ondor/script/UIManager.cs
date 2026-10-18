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

    [Header("UI Panels (현재 씬에 있는 것만 연결해도 됨)")]
    public GameObject titlePanel;
    public GameObject settingsPanel;
    public GameObject lobbyPanel;
    public GameObject shopPanel;
    public GameObject hudPanel;
    public GameObject resultPanel;

    [Header("UI Texts & Sliders (비워둬도 에러 안 남)")]
    public Text hudCoinText;
    public Text hudShieldText;
    public Text lobbySavedCoinText;
    public Text shopStatusText;
    public Text resultSummaryText;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private bool isBgmOn = false;
    private bool isSfxOn = false;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // 멈춰있던 게임 시간 강제 재생
    }

    private void Start()
    {
        // 시작 시 설정창 닫기
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // 만약 현재 씬이 플레이 씬(playe)이면 HUD 패널 자동 켜기
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
        }
    }

    // ==================== 기본 버튼 기능 ====================

    // [시작] 버튼 -> playe 씬으로 이동
    public void OnClickStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // [설정] 버튼 -> 설정창 켜기/끄기
    public void OnClickSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // [되돌아가기] 버튼 -> 설정창 닫기
    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // [게임종료] 버튼 -> 게임 종료
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================== 배경음 / 효과음 강제 변경 ====================

    public void OnClickToggleBgm()
    {
        isBgmOn = !isBgmOn;
        string targetText = isBgmOn ? "배경음 킴" : "배경음 끔";
        ChangeButtonTextOrChild(targetText, isBgmOn, "Music_on", "Music_off");
    }

    public void OnClickToggleSfx()
    {
        isSfxOn = !isSfxOn;
        string targetText = isSfxOn ? "효과음 킴" : "효과음 끔";
        ChangeButtonTextOrChild(targetText, isSfxOn, "Musec_j_on", "Musec_j_off");
    }

    // 버튼 안의 텍스트를 강제로 바꾸거나, on/off 자식 오브젝트를 자동으로 전환
    private void ChangeButtonTextOrChild(string newText, bool isOn, string onObjName, string offObjName)
    {
        GameObject clickedBtn = EventSystem.current.currentSelectedGameObject;
        if (clickedBtn == null) return;

        // 1. 일반 Text 컴포넌트가 있으면 글자 강제 변경
        Text uiText = clickedBtn.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            uiText.text = newText;
        }

        // 2. TextMeshPro 등 다른 텍스트 컴포넌트를 리플렉션으로 찾아 글자 강제 변경 (using TMPro 없이 작동)
        Component[] allComponents = clickedBtn.GetComponentsInChildren<Component>(true);
        foreach (Component comp in allComponents)
        {
            if (comp != null && comp.GetType().Name.Contains("TextMeshPro"))
            {
                var textProp = comp.GetType().GetProperty("text");
                if (textProp != null) textProp.SetValue(comp, newText, null);
            }
        }

        // 3. 만약 버튼 자식으로 Music_on / Music_off 오브젝트가 나뉘어 있다면 켜기/끄기 전환
        Transform onChild = clickedBtn.transform.Find(onObjName);
        Transform offChild = clickedBtn.transform.Find(offObjName);
        if (onChild != null && offChild != null)
        {
            onChild.gameObject.SetActive(isOn);
            offChild.gameObject.SetActive(!isOn);
        }
    }

    // ==================== GameManager 연동용 함수 ====================

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