using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // public ////////////////////////////////
    public GameTimeSettings gameTimeSettings;
    public TextMeshProUGUI timerLabel; // reference to the TextMeshPro component to display the time
    public GameObject loseScreenPrefab; // reference to the lose screen object


    // private ///////////////////////////////
    public double currentTime = 0.0; // is set to the start time at the beginning of the game. decreases as time passes.
    private GameStateManager gameStateManager;

    bool gameOverHandled = false;

    bool isShowingAndTickingTimer = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void TimerStart()
    {
        gameStateManager = PersistentScopeManagers.Instance.GetComponent<GameStateManager>();
        currentTime = gameTimeSettings.StartingTime;
    }

    public string GetCurrentTime()
    {
        int minutes = Mathf.FloorToInt((float)currentTime / 60);
        int seconds = Mathf.FloorToInt((float)currentTime % 60);
        int milliseconds = Mathf.FloorToInt((float)(currentTime * 1000) % 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    void Start()
    {
        TimerStart();
    }

    void Update()
    {
        // only lower the timer if the game is running
        if (gameStateManager.CurrentState != GameState.Playing)
            return;

        timerLabel.SetText(GetCurrentTime());


        if (isShowingAndTickingTimer)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0.0 && gameOverHandled == false)
            {
                // end game
                GameObject canvas = FindAnyObjectByType<Canvas>().gameObject;
                Instantiate(loseScreenPrefab, canvas.transform);
                currentTime = 0.0;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                gameStateManager.CurrentState = GameState.GameOver;

                gameOverHandled = true;
            }
        }

        // Update whether it should be paused
        if (Time.timeScale != 0)
        {

            //timerLabel.gameObject.SetActive(true);
            timerLabel.SetText(GetCurrentTime());

            // Turn timer back to ticking and showing, now that we're unpaused
            isShowingAndTickingTimer = true;
        }
        else
        {
            //timerLabel.gameObject.SetActive(false);


            timerLabel.SetText("");

            // Set to not tick the timer, but keep object active so it can turn itself back on
            isShowingAndTickingTimer = false;
        }
    }
}
