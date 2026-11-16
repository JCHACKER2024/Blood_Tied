using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSwitcher : MonoBehaviour
{
    [SerializeField] private PlayerController[] players;
    [SerializeField] private CameraFollow cameraFollow;
    [Tooltip("Só tem efeito em modo solo. No co-op o Tab nunca troca de personagem.")]
    [SerializeField] private bool allowSwitching = true;

    private int currentIndex;
    private bool switching;
    private bool uiWasOpen;

    private bool CanSwitch => allowSwitching && GameSession.Mode == GameMode.Solo;

    private void Awake()
    {
        // Os players não se empurram nem bloqueiam uns aos outros
        for (int i = 0; i < players.Length; i++)
        {
            for (int j = i + 1; j < players.Length; j++)
            {
                Collider2D a = players[i].GetComponent<Collider2D>();
                Collider2D b = players[j].GetComponent<Collider2D>();
                if (a != null && b != null) Physics2D.IgnoreCollision(a, b);
            }
        }
    }

    private void Start()
    {
        if (players == null || players.Length < 2)
        {
            Debug.LogWarning("PlayerSwitcher: precisa de pelo menos 2 players.");
            return;
        }

        // Começa com o personagem escolhido na cena de seleção
        currentIndex = 0;
        if (GameSession.SelectedCharacter.HasValue)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Character == GameSession.SelectedCharacter.Value)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        for (int i = 0; i < players.Length; i++)
            players[i].SetControlled(i == currentIndex);

        cameraFollow.SetTarget(players[currentIndex].transform);
        cameraFollow.Snap();
    }

    private void Update()
    {
        if (!CanSwitch || switching || ScreenFader.Busy || players == null || players.Length < 2) return;

        // Não troca com inventário/câmara/vista aberta, nem no frame em que o Tab os fecha
        if (InventoryUI.IsOpen || uiWasOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame)
            StartCoroutine(SwitchRoutine());
    }

    private void LateUpdate()
    {
        uiWasOpen = InventoryUI.IsOpen;
    }

    private IEnumerator SwitchRoutine()
    {
        switching = true;
        players[currentIndex].SetControlled(false);

        yield return ScreenFader.Play(() =>
        {
            currentIndex = (currentIndex + 1) % players.Length;
            cameraFollow.SetTarget(players[currentIndex].transform);
            cameraFollow.Snap();
        });

        players[currentIndex].SetControlled(true);
        switching = false;
    }
}