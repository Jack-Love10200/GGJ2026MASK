using UnityEngine;
using UnityEngine.InputSystem;

public class UIParallax : MonoBehaviour
{
    Vector3 startPos;
    Vector3 startEuler;

    public float moveModifier = -50f;
    public float rotModifier = 3f;

    void Start()
    {
        startPos = transform.localPosition;
        startEuler = transform.localEulerAngles;
    }

    void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToViewportPoint(Mouse.current.position.ReadValue());

        if (mousePos.x >= 0 && mousePos.x <= 1 && mousePos.y >= 0 && mousePos.y <= 1)
        {
            // Position Parallax
            Vector3 targetPos = new Vector3(
                startPos.x + (mousePos.x - 0.5f) * moveModifier * (PlayerPrefs.GetFloat("ParallaxIntensity", 50f) / 100),
                startPos.y + (mousePos.y - 0.5f) * moveModifier * (PlayerPrefs.GetFloat("ParallaxIntensity", 50f) / 100),
                startPos.z
            );

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.unscaledDeltaTime * 5f);

            // Rotation Parallax
            Vector3 targetEuler = new Vector3(
                startEuler.x - (mousePos.y - 0.5f) * rotModifier * (PlayerPrefs.GetFloat("ParallaxIntensity", 50f) / 100),
                startEuler.y + (mousePos.x - 0.5f) * rotModifier * (PlayerPrefs.GetFloat("ParallaxIntensity", 50f) / 100),
                startEuler.z
            );

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetEuler), Time.unscaledDeltaTime * 5f);
        }
    }
}
