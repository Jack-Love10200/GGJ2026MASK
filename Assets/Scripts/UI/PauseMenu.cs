using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Post-Processing")]
    public Volume crtVolume;
    public float transitionDuration = 0.1f;

    [Header("Buttons")]
    public Button resumeButton;
    public Button optionsButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Submenus")]
    public Transform submenuParent;
    public GameObject optionsMenuPrefab;
    public GameObject mainMenuConfirmationPrefab;
    public GameObject quitConfirmationPrefab;

    private PauseManager pauseManager;
    private Coroutine fadeCoroutine;
    private List<GameObject> Submenus = new List<GameObject>();

    private void OnEnable()
    {
        FadeInVolume();
    }

    void Start()
    {
        pauseManager = FindAnyObjectByType<PauseManager>();

        resumeButton.onClick.AddListener(ResumeButton);
        optionsButton.onClick.AddListener(OptionsButton);
        mainMenuButton.onClick.AddListener(MainMenuButton);
        quitButton.onClick.AddListener(QuitButton);
    }

    void Update()
    {

    }

    public void ResumeButton()
    {
        DestroySubmenus();
        pauseManager.Unpause();
    }

    public void OptionsButton()
    {
        OptionsMenu options = FindAnyObjectByType<OptionsMenu>();
        if (options != null)
        {
            options.CloseButton();
        }
        else
        {
            Submenus.Add(Instantiate(optionsMenuPrefab, submenuParent));
        }
    }

    public void MainMenuButton()
    {
        Submenus.Add(Instantiate(mainMenuConfirmationPrefab, submenuParent));
    }

    public void QuitButton()
    {
        Submenus.Add(Instantiate(quitConfirmationPrefab, submenuParent));
    }

    public void DestroySubmenus()
    {
        OptionsMenu optionsMenu = FindAnyObjectByType<OptionsMenu>();
        if (optionsMenu != null)
        {
            optionsMenu.CloseButton();
        }

        MainMenuConfirmation mainMenuConfirmation = FindAnyObjectByType<MainMenuConfirmation>();
        if (mainMenuConfirmation != null)
        {
            mainMenuConfirmation.NoButton();
        }

        QuitConfirmation quitConfirmation = FindAnyObjectByType<QuitConfirmation>();
        if (quitConfirmation != null)
        {
            quitConfirmation.NoButton();
        }

        Submenus.Clear();
    }

    #region Post Processing Volume
    private IEnumerator AnimateVolumeWeight(float targetWeight)
    {
        float startWeight = crtVolume.weight;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            crtVolume.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / transitionDuration);

            yield return null;
        }

        crtVolume.weight = targetWeight;
    }

    public void FadeInVolume()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateVolumeWeight(1f));
    }

    public void FadeOutVolume()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateVolumeWeight(0f));
    }
    #endregion

    public void DestroyMe()
    {
        Destroy(this.gameObject);
    }
}
