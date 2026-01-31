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
    public GameState currentState;
    private GameState initialState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        initialState = currentState;
    }

    void Start()
    {
        currentState = initialState;
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
