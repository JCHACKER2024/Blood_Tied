using UnityEngine;

public class Door : Interactable
{
    [Tooltip("Onde o player aparece depois de entrar (a Saída da outra sala).")]
    [SerializeField] private Transform destination;

    public override void Interact(PlayerController player)
    {
        if (destination == null)
        {
            Debug.LogWarning($"{name}: falta definir o Destination.", this);
            return;
        }

        if (ScreenFader.Busy) return;
        StartCoroutine(Teleport(player));
    }

    private System.Collections.IEnumerator Teleport(PlayerController player)
    {
        player.SetControlled(false);

        yield return ScreenFader.Play(() =>
        {
            Vector3 p = player.transform.position;
            player.transform.position = new Vector3(destination.position.x, destination.position.y, p.z);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Snap();
        });

        player.SetControlled(true);
    }
}