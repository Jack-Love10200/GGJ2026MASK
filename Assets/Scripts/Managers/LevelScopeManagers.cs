using Unity.VisualScripting;
using UnityEngine;

// Class for making a game object that holds manger scripts which only exist till a level is reset or changed
// Not intended to have any managing logic itself, but to have manager scripts attached to it, and be an easy way to get them
public class LevelScopeManagers : MonoBehaviour
{
    public static LevelScopeManagers Instance;

    private void Awake()
    {
        // Set the instance value so we can access it
        Instance = this;

        // This scope of managers doesn't persist between levels, and so we want it to destroy on load like normal
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
