using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float offsetX = 0f;

    private Camera cam;
    private RoomBounds[] rooms;
    private float velocityX;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        rooms = FindObjectsByType<RoomBounds>();
    }

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

        RoomBounds room = GetRoom(target.position.x);
        if (room == null) return x;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float min = room.MinX + halfWidth;
        float max = room.MaxX - halfWidth;

        // Sala mais estreita que o ecrã: câmara fixa no centro da sala
        if (min > max) return (room.MinX + room.MaxX) * 0.5f;

        return Mathf.Clamp(x, min, max);
    }

    private RoomBounds GetRoom(float x)
    {
        foreach (RoomBounds r in rooms)
        {
            if (r != null && r.Contains(x)) return r;
        }
        return null;
    }
}