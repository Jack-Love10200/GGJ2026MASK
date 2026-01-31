using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
  void Start()
  {
    
  }
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