using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip startBGM;      // 시작 화면 BGM
    public AudioClip playBGM;       // 플레이 화면 BGM
    public AudioClip endingBGM;     // 엔딩 화면 BGM

    [Header("SFX Clips")]
    public AudioClip buttonClickSFX;// UI 버튼 클릭
    public AudioClip coinSFX;       // 코인 획득
    public AudioClip gameOverSFX;   // 게임 오버(실패)
    public AudioClip gameClearSFX;  // 배달 완료(성공)

    public bool IsBgmOn { get; private set; } = true;
    public bool IsSfxOn { get; private set; } = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.mute = !IsBgmOn;
        bgmSource.Play();
    }

    public void SetBGMActive(bool isOn)
    {
        IsBgmOn = isOn;
        if (bgmSource != null) bgmSource.mute = !IsBgmOn;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null || !IsSfxOn) return;
        sfxSource.PlayOneShot(clip);
    }

    public void SetSFXActive(bool isOn)
    {
        IsSfxOn = isOn;
        if (sfxSource != null) sfxSource.mute = !IsSfxOn;
    }

    public void PlayButtonClick() => PlaySFX(buttonClickSFX);
    public void PlayCoinSound() => PlaySFX(coinSFX);
    public void PlayGameOverSound() => PlaySFX(gameOverSFX);
    public void PlayGameClearSound() => PlaySFX(gameClearSFX);
}