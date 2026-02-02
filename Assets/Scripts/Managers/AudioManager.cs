using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioClip music;

    [Header("Settings")]
    public AudioMixer audioMixer;
    [Header("Mixer Groups")]
    public AudioMixerGroup sfxMixerGroup;
    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            if (musicSource != null && music != null)
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        ApplySavedVolumeSettings();
    }

    public void ApplySavedVolumeSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetMixerVolume("MasterVolume", master);
        SetMixerVolume("MusicVolume", music);
        SetMixerVolume("SFXVolume", sfx);
    }

    public void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;

        float dbValue = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, dbValue);
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        EnsureSources();
        if (sfxSource == null)
            return;

        if (sfxMixerGroup != null && sfxSource.outputAudioMixerGroup != sfxMixerGroup)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

        sfxSource.PlayOneShot(clip, volume);
    }

    private void EnsureSources()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (sfxMixerGroup != null && sfxSource.outputAudioMixerGroup != sfxMixerGroup)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }
}
