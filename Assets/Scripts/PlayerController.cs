using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;

    Vector2 moveInput;

    void Update()
    {
        // --- Get directional input ---
        moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            moveInput.y += 1f;
        if (Keyboard.current.sKey.isPressed)
            moveInput.y -= 1f;
        if (Keyboard.current.aKey.isPressed)
            moveInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed)
            moveInput.x += 1f;

        // --- Normalize diagonal movement ---
        if (moveInput.sqrMagnitude > 1)
            moveInput.Normalize();

        // --- Move the player ---
        Vector3 moveDelta = new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed * Time.deltaTime;
        transform.position += moveDelta;

        // --- Rotate to face movement direction ---
        if (moveInput != Vector2.zero)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // adjust -90f if your sprite faces up by default
        }
    }
}
