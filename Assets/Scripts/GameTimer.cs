using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // public ////////////////////////////////
    public double startTime = 120.0; // start time in seconds
    public TextMeshProUGUI timerLabel; // reference to the TextMeshPro component to display the time
    public GameObject loseScreenPrefab; // reference to the lose screen object

    // private ///////////////////////////////
    public double currentTime = 0.0; // is set to the start time at the beginning of the game. decreases as time passes.
    private GameStateManager gameStateManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void TimerStart()
    {
        gameStateManager = PersistentScopeManagers.Instance.GetComponent<GameStateManager>();
        currentTime = startTime;
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

        currentTime -= Time.deltaTime;

        if (currentTime <= 0.0)
        {
            // end game
            GameObject canvas = FindAnyObjectByType<Canvas>().gameObject;
            Instantiate(loseScreenPrefab, canvas.transform);
            currentTime = 0.0;
            gameStateManager.CurrentState = GameState.GameOver;
        }
    }
}
