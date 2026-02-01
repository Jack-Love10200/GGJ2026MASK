using UnityEngine;

public class HUD : MonoBehaviour
{
    public float canvasPlaneDistance = 1f;

    private void OnEnable()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = canvasPlaneDistance;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
