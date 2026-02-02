using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioClip music;

    [Header("Settings")]
    public AudioMixer audioMixer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GetComponent<AudioSource>().clip = music;
            GetComponent<AudioSource>().Play();
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
}