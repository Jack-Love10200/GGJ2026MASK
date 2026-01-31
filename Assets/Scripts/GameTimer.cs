using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // public ////////////////////////////////
    public double startTime = 120.0; // start time in seconds
    public TextMeshProUGUI timeText; // reference to the TextMeshPro component to display the time
    public GameObject loseScreen; // reference to the lose screen object

    // private ///////////////////////////////
    private double currentTime = 0.0; // is set to the start time at the beginning of the game. decreases as time passes.

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void TimerStart()
    {
        currentTime = startTime;
    }

    void Start()
    {
        TimerStart();
    }

    // Update is called once per frame
    void Update()
    {
        // only lower the timer if the game is running
        if (GameStateManager.Instance.currentState != GameState.Playing)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0.0)
        {
            // end game
            loseScreen.SetActive(true);
            currentTime = 0.0;
            GameStateManager.Instance.currentState = GameState.GameOver;
        }

        timeText.text = currentTime.ToString("F2");
    }
}
