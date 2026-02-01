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
    // public ///////////////////////////////////////////////////////
    public GameState CurrentState
    {
        set 
        {
            m_CurrentState = value;
            if (m_OnGameStateSwitchEvent != null)
                m_OnGameStateSwitchEvent.Invoke(value);
        }

        get
        {
            return m_CurrentState;
        }
    }

    public Action<GameState> OnStateSwitch
    {
        get { return m_OnGameStateSwitchEvent; }

        set { m_OnGameStateSwitchEvent += value; }
    }

    // private ////////////////////////////////////////////////////
    [SerializeField]
    private GameState m_CurrentState;
    private GameState m_InitialState;
    private Action<GameState> m_OnGameStateSwitchEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        m_InitialState = m_CurrentState;
    }

    void Start()
    {
        m_CurrentState = m_InitialState;
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
