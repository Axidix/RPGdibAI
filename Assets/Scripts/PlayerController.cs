using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Animator anim;

    string lastPlayedClip = "";

    Rigidbody2D rb;
    Vector2 moveInput;
    Vector2 moveVelocity;

    // remembers the last cardinal direction the player moved toward
    // 0 = Down, 1 = Left, 2 = Right, 3 = Up
    int lastMovedDir = 0;

    void Awake()
    {
        anim = anim ? anim : GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (anim == null) Debug.LogError("Animator not found on " + name);
        if (rb == null) Debug.LogError("Rigidbody2D not found on " + name);
    }

    void Update()
    {
        // --- Get directional input ---
        moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;

        // --- Remove diagonal movement: keep only the dominant axis ---
        if (Mathf.Abs(moveInput.x) > 0f && Mathf.Abs(moveInput.y) > 0f)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                moveInput.y = 0f;
            else if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
                moveInput.x = 0f;
            else
                moveInput.y = 0f; // tie-breaker: prefer horizontal movement
        }

        // store velocity for physics update
        moveVelocity = moveInput * moveSpeed;

        // update last moved direction whenever there is input
        if (moveInput != Vector2.zero)
            lastMovedDir = GetCardinalDirection(moveInput);

        // --- Decide facing direction (use input to decide movement state so animation responds immediately) ---
        bool isMoving = moveInput != Vector2.zero;
        int dir = isMoving ? GetCardinalDirection(moveInput) : lastMovedDir;

        // --- Play correct animation (Idle vs Walk + direction) ---
        if (isMoving)
            PlayAnimForDir("walk", dir);
        else
            PlayAnimForDir("idle", dir);
    }

    void FixedUpdate()
    {
        // Move via Rigidbody2D only (prevents jitter)
        if (rb != null)
            rb.MovePosition(rb.position + moveVelocity * Time.fixedDeltaTime);
    }

    // 0 = Down, 1 = Left, 2 = Right, 3 = Up
    int GetCardinalDirection(Vector2 v)
    {
        if (v == Vector2.zero) return 0; // default facing down if idle and unknown

        // choose dominant axis
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x > 0 ? 2 : 1; // right or left
        else
            return v.y > 0 ? 3 : 0; // up or down
    }

    void PlayAnimForDir(string baseName, int dir)
    {
        string dirName = dir switch
        {
            0 => "down",
            1 => "left",
            2 => "right",
            3 => "up",
            _ => "down"
        };

        string clipName = $"{baseName}_{dirName}";

        // If it's the same clip that's already being requested/played, do nothing.
        if (clipName == lastPlayedClip)
            return;

        // Optional debug
        Debug.Log("Switching animation to: " + clipName);

        // Try to play/crossfade only once when it changes
        if (anim.HasState(0, Animator.StringToHash(clipName)))
        {
            anim.CrossFade(clipName, 0.08f);
            lastPlayedClip = clipName;
        }
        else
        {
            Debug.LogWarning("Animator state not found: " + clipName);
            // don't update lastPlayedClip so future correct names can still trigger
        }
    }
}
