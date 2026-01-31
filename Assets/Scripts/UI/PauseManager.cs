using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PauseManager : MonoBehaviour
{
    [Header("Settings")]
    public InputActionReference pauseAction;
    public GameObject pauseMenuPrefab;
    public float canvasPlaneDistance = 1f;

    [Header("Post-Processing")]
    public Volume crtVolume;
    public float transitionDuration = 0.5f;

    private GameObject currentPauseMenu = null;
    private float recordedTimeScale = 1f;
    private bool canPause = true;
    private bool isPaused = false;

    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= OnPausePerformed;
        pauseAction.action.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (!canPause)
            return;

        if (isPaused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        //Record and set time scale
        recordedTimeScale = Time.timeScale;
        Time.timeScale = 0;

        //Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //Instantiate pause menu
        currentPauseMenu = Instantiate(pauseMenuPrefab);
        Canvas canvas = currentPauseMenu.GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = canvasPlaneDistance;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateVolumeWeight(1f));
    }

    public void Unpause()
    {
        isPaused = false;

        Time.timeScale = recordedTimeScale;

        //Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Destroy(currentPauseMenu);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateVolumeWeight(0f));
    }

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

    public void SetCanPause(bool value)
    {
        canPause = value;
    }
}
