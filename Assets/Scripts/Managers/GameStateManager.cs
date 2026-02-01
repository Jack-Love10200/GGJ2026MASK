using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

public class GameStateManager : MonoBehaviour
{
    // private /////////////////////////////
    [SerializeField]
    private GameState m_CurrentState;

    public GameState CurrentState
    {
        set 
        {
            m_CurrentState = value;
            if (onGameStateSwitchEvent != null)
                onGameStateSwitchEvent.Invoke(value);
        }

        get
        {
            return m_CurrentState;
        }
    }

    private GameState initialState;

    public Action<GameState> OnStateSwitch
    {
        get { return onGameStateSwitchEvent; }

        set { onGameStateSwitchEvent += value; }
    }

    private Action<GameState> onGameStateSwitchEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        initialState = m_CurrentState;
    }



    void Start()
    {
        m_CurrentState = initialState;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
