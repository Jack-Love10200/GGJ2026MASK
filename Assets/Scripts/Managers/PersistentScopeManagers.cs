using UnityEngine;

// Class for making a game object persistent across levels
// Not intended to have any managing logic itself, but to have manager scripts attached to it, and be an easy way to get them
public class PersistentScopeManagers : MonoBehaviour
{

    public static PersistentScopeManagers Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
