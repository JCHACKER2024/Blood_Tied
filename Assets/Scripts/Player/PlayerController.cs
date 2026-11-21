using Unity.Netcode;
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
    private NetworkObject netObject;
    private float moveInput;
    private string currentState = "";
    private bool controlled = true;

    // Jogador do outro PC: o movimento deduz-se pela posição que chega pela rede
    private Vector3 lastRemotePos;
    private float remoteMoveTimer;
    private float remoteDirection = 1f;

    public CharacterType Character => character;

    // Online: só o PC dono deste personagem o controla. Solo: sempre verdadeiro.
    private bool IsNetworked => GameSession.Mode != GameMode.Solo && netObject != null;
    private bool OwnedLocally => !IsNetworked || netObject.IsOwner;

    // Fica "não controlado" enquanto o inventário está aberto
    public bool IsControlled => controlled && OwnedLocally && !InventoryUI.IsOpen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        netObject = GetComponent<NetworkObject>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        lastRemotePos = transform.position;
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
        // Personagem do outro PC: animação e direção pela posição recebida
        if (!OwnedLocally)
        {
            UpdateRemote();
            return;
        }

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

        ApplyFlip();
        UpdateAnimation();
    }

    private void UpdateRemote()
    {
        float dx = transform.position.x - lastRemotePos.x;
        lastRemotePos = transform.position;

        float speed = Mathf.Abs(dx) / Mathf.Max(Time.deltaTime, 0.0001f);

        // Ignora saltos grandes (portas) e movimento mínimo
        if (speed > 0.2f && Mathf.Abs(dx) < 1f)
        {
            remoteMoveTimer = 0.12f;
            remoteDirection = dx > 0f ? 1f : -1f;
        }
        else
        {
            remoteMoveTimer -= Time.deltaTime;
        }

        // O pequeno temporizador evita que a animação pisque entre atualizações da rede
        moveInput = remoteMoveTimer > 0f ? remoteDirection : 0f;

        ApplyFlip();
        UpdateAnimation();
    }

    private void ApplyFlip()
    {
        if (spriteRenderer == null || moveInput == 0f) return;

        bool goingLeft = moveInput < 0f;
        spriteRenderer.flipX = spriteFacesRight ? goingLeft : !goingLeft;
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
        // O personagem do outro PC é movido pela rede, não por nós
        if (!OwnedLocally) return;

        Vector2 vel = rb.linearVelocity;
        vel.x = moveInput * moveSpeed;
        rb.linearVelocity = vel;
    }
}