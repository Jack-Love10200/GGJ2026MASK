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

    private GameObject currentPauseMenu = null;
    private float recordedTimeScale = 1f;
    private bool canPause = true;
    private bool isPaused = false;

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
    }

    public void Unpause()
    {
        isPaused = false;

        Time.timeScale = recordedTimeScale;

        //Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        //Play close animation
        currentPauseMenu.GetComponent<Animator>().SetTrigger("Close");
        currentPauseMenu.GetComponent<PauseMenu>().DestroySubmenus();
    }

    public void SetCanPause(bool value)
    {
        canPause = value;
    }
}
