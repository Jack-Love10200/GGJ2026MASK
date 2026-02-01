using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleTestMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool lockCursor = true;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        ApplyCursorState();
    }

    private void OnDisable()
    {
        if (!lockCursor)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        if (Mouse.current == null)
            return;

        float mouseX = Mouse.current.delta.ReadValue().x;
        if (Mathf.Abs(mouseX) <= Mathf.Epsilon)
            return;

        float yaw = mouseX * mouseSensitivity;
        transform.Rotate(0f, yaw, 0f, Space.Self);
    }

    private void HandleMove()
    {
        if (Keyboard.current == null)
            return;

        float inputX = 0f;
        if (Keyboard.current.aKey.isPressed)
            inputX -= 1f;
        if (Keyboard.current.dKey.isPressed)
            inputX += 1f;

        float inputZ = 0f;
        if (Keyboard.current.sKey.isPressed)
            inputZ -= 1f;
        if (Keyboard.current.wKey.isPressed)
            inputZ += 1f;

        Vector3 move = transform.right * inputX + transform.forward * inputZ;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        Vector3 delta = move * (moveSpeed * Time.deltaTime);

        if (controller != null && controller.enabled)
            controller.Move(delta);
        else
            transform.position += delta;
    }

    private void ApplyCursorState()
    {
        if (!lockCursor)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
