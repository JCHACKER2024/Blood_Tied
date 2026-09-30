using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float offsetX = 0f;

    [Header("Limites (opcional)")]
    [SerializeField] private bool useLimits = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;

    private float velocityX;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Snap()
    {
        if (target == null) return;

        velocityX = 0f;
        Vector3 pos = transform.position;
        pos.x = GetTargetX();
        transform.position = pos;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.SmoothDamp(pos.x, GetTargetX(), ref velocityX, smoothTime);
        transform.position = pos;
    }

    private float GetTargetX()
    {
        float x = target.position.x + offsetX;
        return useLimits ? Mathf.Clamp(x, minX, maxX) : x;
    }
}