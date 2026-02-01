using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // public ///////////////////////
    public uint KillQuota = 0;
    public GameObject winScreenPrefab;

    // private //////////////////////
    private GameStateManager gsm;
    private uint m_KillCount = 0;

    [ContextMenu("Increment Count")]
    public void TrackKill()
    {
        m_KillCount++;

        if (m_KillCount >= KillQuota)
        {
            gsm.CurrentState = GameState.Win;

            SpawnWinScreen();
        }
    }

    void Start()
    {
        gsm = PersistentScopeManagers.Instance.GetComponent<GameStateManager>();

        m_KillCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    // Helper functions
    void SpawnWinScreen()
    {
        Canvas canvasObj = FindFirstObjectByType<Canvas>();
        Instantiate(winScreenPrefab, canvasObj.transform);

    }

}
