using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // public ///////////////////////
    public uint KillQuota = 0;

    // private //////////////////////
    private GameStateManager gsm;
    private uint m_KillCount = 0;

    [ContextMenu("Increment Count")]
    public void TrackKill()
    {
        m_KillCount++;

        if (m_KillCount == KillQuota)
        {
            gsm.CurrentState = GameState.Win;
        }
    }

    void Start()
    {
        gsm = PersistentScopeManagers.Instance.GetComponent<GameStateManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
