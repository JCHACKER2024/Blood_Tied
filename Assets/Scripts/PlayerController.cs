using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum Scheme { WASD, Arrows }

    [Header("Controls")]
    [SerializeField] private Scheme scheme = Scheme.WASD;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRight = false;
    [SerializeField] private string walkStateName = "Alex_Walk";

    private Rigidbody2D rb;
    private float moveInput;
    private bool wasMoving = true;

    public bool IsControlled { get; private set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetControlled(bool value)
    {
        IsControlled = value;

        if (!value)
        {
            moveInput = 0f;
            UpdateAnimation();
        }
    }

    private void Update()
    {
        if (!IsControlled)
        {
            moveInput = 0f;
            UpdateAnimation();
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool left, right;

        if (scheme == Scheme.WASD)
        {
            left = kb.aKey.isPressed;
            right = kb.dKey.isPressed;
        }
        else
        {
            left = kb.leftArrowKey.isPressed;
            right = kb.rightArrowKey.isPressed;
        }

        moveInput = (right ? 1f : 0f) - (left ? 1f : 0f);

        if (spriteRenderer != null && moveInput != 0f)
        {
            bool goingLeft = moveInput < 0f;
            spriteRenderer.flipX = spriteFacesRight ? goingLeft : !goingLeft;
        }

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveInput != 0f;
        if (isMoving == wasMoving) return;
        wasMoving = isMoving;

        if (isMoving)
        {
            animator.speed = 1f;
        }
        else
        {
            animator.Play(walkStateName, 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    private void FixedUpdate()
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = moveInput * moveSpeed;
        rb.linearVelocity = vel;
    }
}