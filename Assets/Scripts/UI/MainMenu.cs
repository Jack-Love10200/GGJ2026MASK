using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Submenus")]
    public Transform submenusParent;
    public GameObject optionsMenuPrefab;
    public GameObject creditsMenuPrefab;
    public GameObject quitConfirmationPrefab;

    void Start()
    {
        playButton.onClick.AddListener(PlayButton);
        optionsButton.onClick.AddListener(OptionsButton);
        creditsButton.onClick.AddListener(CreditsButton);
        quitButton.onClick.AddListener(QuitButton);
    }

    void Update()
    {
        
    }

    public void PlayButton()
    {
        SceneManager.LoadScene("TestingScene");
    }
    
    public void OptionsButton()
    {
        Instantiate(optionsMenuPrefab, submenusParent);
    }

    public void CreditsButton()
    {
        Instantiate(creditsMenuPrefab, submenusParent);
    }

    public void QuitButton()
    {
        Instantiate(quitConfirmationPrefab, submenusParent);
    }
}
