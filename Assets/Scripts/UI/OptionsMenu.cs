using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Button closeButton;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        closeButton.onClick.AddListener(CloseButton);
    }

    void Update()
    {
        
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
