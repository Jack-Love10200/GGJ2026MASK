using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    public Button closeButton;

    [Header("Video")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Audio")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;


    private Resolution[] filteredResolutions;

    void Start()
    {
        closeButton.onClick.AddListener(CloseButton);

        InitializeVideoSettings();
        InitializeAudioSettings();
    }

    void Update()
    {
        
    }

    #region Video Settings
    public void InitializeVideoSettings()
    {
        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            string option = allResolutions[i].width + " x " + allResolutions[i].height;

            if (!options.Contains(option))
            {
                options.Add(option);
                uniqueResolutions.Add(allResolutions[i]);

                if (allResolutions[i].width == Screen.currentResolution.width && allResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = uniqueResolutions.Count - 1;
                }
            }
        }

        filteredResolutions = uniqueResolutions.ToArray();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (filteredResolutions == null || resolutionIndex >= filteredResolutions.Length)
            return;

        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    #endregion

    public void InitializeAudioSettings()
    {
        float masterVal = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVal = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVal = PlayerPrefs.GetFloat("SFXVolume", 1f);

        masterSlider.value = masterVal;
        musicSlider.value = musicVal;
        sfxSlider.value = sfxVal;

        SetVolume("MasterVolume", masterVal);
        SetVolume("MusicVolume", musicVal);
        SetVolume("SFXVolume", sfxVal);

        masterSlider.onValueChanged.AddListener((val) => OnVolumeChanged("MasterVolume", val));
        musicSlider.onValueChanged.AddListener((val) => OnVolumeChanged("MusicVolume", val));
        sfxSlider.onValueChanged.AddListener((val) => OnVolumeChanged("SFXVolume", val));
    }

    private void OnVolumeChanged(string parameterName, float sliderValue)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMixerVolume(parameterName, sliderValue);
        }

        PlayerPrefs.SetFloat(parameterName, sliderValue);
        PlayerPrefs.Save();
    }

    private void SetVolume(string parameterName, float sliderValue)
    {
        float dbValue = (sliderValue <= 0.01f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, dbValue);
    }

    public void CloseButton()
    {
        GetComponent<Animator>().SetTrigger("Close");
    }

    public void DestroyMe()
    {
        Destroy(this.gameObject);
    }
}
