using UnityEngine;

public class Player : MonoBehaviour
{
  // Start is called once before the first execution of Update after the MonoBehaviour is created;

  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    transform.Translate(transform.forward, Space.Self);

    if (Input.GetKeyDown(KeyCode.A))
    {
      transform.Rotate(0, -90, 0);
    }

    if (Input.GetKeyDown(KeyCode.D))
    {
      transform.Rotate(0, 90, 0);
    }
  }
}
