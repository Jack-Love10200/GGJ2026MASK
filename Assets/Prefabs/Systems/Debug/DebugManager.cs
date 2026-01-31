using UnityEngine;
using UnityEngine.Rendering;



public class DebugManager : MonoBehaviour
{


    // Public variables ////////////////////////////////////////////////////////////////////////

    // Public instance value for use as a singleton class
    public static DebugManager Instance;


    // Getters and setters /////////////////////////////////////////////////////////////////////

    // public variable for changing if debug is on
    public bool IsDebugOn { 
        get { return mIsDebugOn;}
        set { mIsDebugOn = value; SetIsDebugOn(mIsDebugOn); }
    }
    

    // Private variables ///////////////////////////////////////////////////////////////////////

    bool mIsDebugOn;


    private void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Helper functions
    void SetIsDebugOn(bool newIsDebugOn)
    {

    }

}
