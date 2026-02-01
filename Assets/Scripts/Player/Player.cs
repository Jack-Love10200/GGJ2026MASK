using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public enum InputDirection
{
  Left,
  Right,
  Forward,
}

public class Player : MonoBehaviour
{

  enum MovementState
  {
    Moving,
    Turning,
  }

  private struct TurnData
  {
    public float angle;

    public Quaternion startRotation;
    public Quaternion endRotation;

    public Vector3 startPosition;
    public Vector3 endPosition;
    public float timeInTurn;
  }


  [Header("Settings")]
  public float Speed;
  public float TurnTime;
  private TurnData currentTurn;

  private MovementState currentState = MovementState.Moving;


  InputDirection desiredDirection = InputDirection.Forward;

  void Start()
  {

  }
  void Update()
  {
    if (currentState == MovementState.Moving)
    {
      transform.Translate(Vector3.forward * Speed * Time.deltaTime);
    }
    else if (currentState == MovementState.Turning)
    {


      if (currentTurn.timeInTurn > TurnTime)
      {
        // Make sure end result is exactly accurate
        transform.position = Vector3.Lerp(currentTurn.startPosition, currentTurn.endPosition, currentTurn.timeInTurn / TurnTime);
        transform.rotation = Quaternion.Slerp(currentTurn.startRotation, currentTurn.endRotation, 1.0f);

        currentState = MovementState.Moving;

      }

            //lerp postion and rotation to the target
      transform.position = Vector3.Lerp(currentTurn.startPosition, currentTurn.endPosition, currentTurn.timeInTurn / TurnTime);
      transform.rotation = Quaternion.Slerp(currentTurn.startRotation, currentTurn.endRotation, currentTurn.timeInTurn / TurnTime);

            currentTurn.timeInTurn += Time.deltaTime;
        }
  }

  void OnTriggerEnter(Collider other)
  {
    currentState = MovementState.Turning;

    Debug.Log("Trigger enter");

    if (other.TryGetComponent<JunctionTrigger>(out JunctionTrigger junction))
    {
      List<JunctionTrigger.DirectionOption> options = junction.OpenDirections;

      if (options.Count <= 0)
        Debug.LogWarning("Junction has no open directions");

      int optionIndex = options.FindIndex(option => option.inputDirection == desiredDirection);
      if (optionIndex == -1) //player chose a direction they cant go, Jack gets to pick where they go now
      {
        optionIndex = Random.Range(0, options.Count - 1);
      }

      TurnData turn = new TurnData();
      turn.angle = options[optionIndex].angle;
      turn.startPosition = transform.position;
      turn.endPosition = junction.Destination.position;
      turn.startRotation = transform.rotation;
      turn.endRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + turn.angle, 0);
      turn.timeInTurn = 0f;

      currentTurn = turn;
    }

    else
      Debug.LogWarning("Player collided with not junction object");
  }

  public void OnMoveForward(InputAction.CallbackContext context)
  {
    if (context.started)
      desiredDirection = InputDirection.Forward;
  }

  public void OnMoveLeft(InputAction.CallbackContext context)
  {
    if (context.started)
      desiredDirection = InputDirection.Left;
  }

  public void OnMoveRight(InputAction.CallbackContext context)
  {
    if (context.started)
      desiredDirection = InputDirection.Right;
  }
}