using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    public static bool Busy { get; private set; }

    [SerializeField] private float fadeDuration = 0.08f;
    [SerializeField] private float blackHoldTime = 0f;

    private CanvasGroup group;

    private void Awake()
    {
        Instance = this;
        Busy = false;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    // Faz fade para preto, executa midAction (ex: teleporte) e volta ao normal.
    public static IEnumerator Play(Action midAction)
    {
        if (Instance == null)
        {
            midAction?.Invoke();
            yield break;
        }

        yield return Instance.Transition(midAction);
    }

    private IEnumerator Transition(Action midAction)
    {
        Busy = true;

        yield return Fade(0f, 1f);
        midAction?.Invoke();
        if (blackHoldTime > 0f) yield return new WaitForSecondsRealtime(blackHoldTime);
        yield return Fade(1f, 0f);

        Busy = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }
}