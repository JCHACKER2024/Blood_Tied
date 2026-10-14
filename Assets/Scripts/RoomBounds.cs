using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;

    // Limite interior: onde a parede esquerda acaba e a direita começa
    public float MinX => GetBounds(leftWall).max.x;
    public float MaxX => GetBounds(rightWall).min.x;

    // Indica se um X está dentro desta sala (paredes incluídas)
    public bool Contains(float x)
    {
        return x >= GetBounds(leftWall).min.x && x <= GetBounds(rightWall).max.x;
    }

    private static Bounds GetBounds(Transform t)
    {
        Collider2D c = t.GetComponent<Collider2D>();
        if (c != null) return c.bounds;

        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds;

        return new Bounds(t.position, Vector3.zero);
    }
}