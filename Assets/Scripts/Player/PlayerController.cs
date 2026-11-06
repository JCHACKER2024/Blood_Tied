using UnityEngine;
using UnityEngine.InputSystem;

public enum CharacterType { Axel, Mike }

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum Scheme { WASD, Arrows }

    [Header("Personagem")]
    [SerializeField] private CharacterType character = CharacterType.Axel;

    [Header("Controls")]
    [SerializeField] private Scheme scheme = Scheme.WASD;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "Alex_Idle";
    [SerializeField] private string walkStateName = "Alex_Walk";

    [Header("Animation Speed")]
    [SerializeField] private float idleAnimSpeed = 1f;
    [SerializeField] private float walkAnimSpeed = 1f;

    private Rigidbody2D rb;
    private float moveInput;
    private string currentState = "";
    private bool controlled = true;

    public CharacterType Character => character;

    // Fica "não controlado" enquanto o inventário está aberto
    public bool IsControlled => controlled && !InventoryUI.IsOpen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetControlled(bool value)
    {
        controlled = value;

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
        if (animator == null || animator.runtimeAnimatorController == null) return;

        bool isMoving = moveInput != 0f;
        string desired = isMoving ? walkStateName : idleStateName;
        if (desired == currentState) return;

        currentState = desired;

        if (!animator.HasState(0, Animator.StringToHash(desired)))
        {
            Debug.LogWarning($"{name}: o estado '{desired}' não existe no Animator. Confirma o nome no Player Controller.", this);
            return;
        }

        animator.speed = isMoving ? walkAnimSpeed : idleAnimSpeed;
        animator.Play(desired, 0, 0f);
    }

    private void FixedUpdate()
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = moveInput * moveSpeed;
        rb.linearVelocity = vel;
    }
}