using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Look")]
    public float lookSensitivity = 0.1f;

    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pitch = transform.eulerAngles.x;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    /// Rotates the transform based on mouse delta.
    /// Yaw rotates around world Y, pitch is clamped to prevent flipping.
    void HandleLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue();
        float mouseX = delta.x * lookSensitivity;
        float mouseY = delta.y * lookSensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        float yaw = transform.eulerAngles.y + mouseX;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// Moves relative to camera facing (WASD), with Space for up
    /// and Left Shift for down in world space.
    void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 move = Vector3.zero;

        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.spaceKey.isPressed) move += Vector3.up;
        if (keyboard.leftShiftKey.isPressed) move += Vector3.down;

        if (move.sqrMagnitude > 0f)
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
    }
}
