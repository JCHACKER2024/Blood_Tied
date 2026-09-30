using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSwitcher : MonoBehaviour
{
    [SerializeField] private PlayerController[] players;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float blackHoldTime = 0.1f;

    private int currentIndex;
    private bool switching;

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

        for (int i = 0; i < players.Length; i++)
            players[i].SetControlled(i == currentIndex);

        cameraFollow.SetTarget(players[currentIndex].transform);
        cameraFollow.Snap();

        if (fade != null) fade.alpha = 0f;
    }

    private void Update()
    {
        if (switching || players == null || players.Length < 2) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame)
            StartCoroutine(SwitchRoutine());
    }

    private IEnumerator SwitchRoutine()
    {
        switching = true;
        players[currentIndex].SetControlled(false);

        yield return Fade(0f, 1f);

        currentIndex = (currentIndex + 1) % players.Length;
        cameraFollow.SetTarget(players[currentIndex].transform);
        cameraFollow.Snap();

        yield return new WaitForSecondsRealtime(blackHoldTime);
        yield return Fade(1f, 0f);

        players[currentIndex].SetControlled(true);
        switching = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fade == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fade.alpha = to;
    }
}