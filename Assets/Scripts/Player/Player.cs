using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
  // Start is called once before the first execution of Update after the MonoBehaviour is created;

  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    transform.Translate(transform.forward, Space.World);
  }

  public void OnMoveLeft(InputAction.CallbackContext context)
  {
    if (context.started)
    {
      transform.Rotate(0, -90, 0);
    }
  }

  public void OnMoveRight(InputAction.CallbackContext context)
  {
    if (context.started)
    {
      transform.Rotate(0, 90, 0);
    }
  }
}