using Unity.Netcode;
using Unity.Netcode.Components;
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
            Vector3 newPos = new Vector3(destination.position.x, destination.position.y, p.z);

            // Online: avisa a rede de que foi um salto, para o outro PC não o ver a deslizar até lá
            NetworkTransform nt = player.GetComponent<NetworkTransform>();
            if (GameSession.Mode != GameMode.Solo && nt != null && nt.IsSpawned && nt.IsOwner)
                nt.Teleport(newPos, player.transform.rotation, player.transform.localScale);
            else
                player.transform.position = newPos;

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Snap();
        });

        player.SetControlled(true);
    }
}