using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // public ////////////////////////////////
    public double startTime = 120.0; // start time in seconds
    public TextMeshProUGUI debugText; // reference to the TextMeshPro component to display the time
    public GameObject loseScreenPrefab; // reference to the lose screen object

    // private ///////////////////////////////
    private double currentTime = 0.0; // is set to the start time at the beginning of the game. decreases as time passes.
    private GameStateManager gsm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void TimerStart()
    {
        gsm = PersistentScopeManagers.Instance.GetComponent<GameStateManager>();
        currentTime = startTime;
    }

    string GetCurrentTime()
    {
        return currentTime.ToString("F2");
    }

    void Start()
    {
        TimerStart();
    }

    // Update is called once per frame
    void Update()
    {
        // only lower the timer if the game is running
        if (gsm.CurrentState != GameState.Playing)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0.0)
        {
            // end game
            GameObject canvas = FindAnyObjectByType<Canvas>().gameObject;
            Instantiate(loseScreenPrefab, canvas.transform);
            currentTime = 0.0;
            gsm.CurrentState = GameState.GameOver;
        }

        if (debugText)
            debugText.text = currentTime.ToString("F2");
    }
}
