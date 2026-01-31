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

    // public //////////////////////////////
    public static GameStateManager Instance;

    // private /////////////////////////////
    public GameState currentState = GameState.Playing;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = GameState.Playing;
        Instance = this;
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
