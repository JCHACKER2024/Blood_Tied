using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class NetTestPlayer : NetworkBehaviour
{
    [SerializeField] private float speed = 3f;

    private void Update()
    {
        // Só o dono do objeto o controla; o outro PC só o vê
        if (!IsOwner) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        transform.position += Vector3.right * x * speed * Time.deltaTime;
    }
}