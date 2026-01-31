using UnityEngine;

// Class for making a game object persistent across levels
// Not intended to have any managing logic itself, but to have manager scripts attached to it, and be an easy way to get them
public class PersistentScopeManagers : MonoBehaviour
{

    public static PersistentScopeManagers Instance;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // if an instance already exists, destroy this one, so that we can put this manager in each level, but don't get duplicates created
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            // Set to not destroy on load so that we have it always persistent
            DontDestroyOnLoad(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
