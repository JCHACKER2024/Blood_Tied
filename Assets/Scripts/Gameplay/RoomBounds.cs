using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;

    // Limite interior: onde a parede esquerda acaba e a direita começa
    public float MinX => GetBounds(leftWall).max.x;
    public float MaxX => GetBounds(rightWall).min.x;

    // Centro vertical da sala (a câmara fica a esta altura)
    public float CenterY => Area.center.y;

    private Bounds Area
    {
        get
        {
            Bounds b = GetBounds(leftWall);
            b.Encapsulate(GetBounds(rightWall));
            return b;
        }
    }

    // Indica se uma posição está dentro desta sala (paredes incluídas)
    public bool Contains(Vector2 position)
    {
        Bounds a = Area;
        return position.x >= a.min.x && position.x <= a.max.x &&
               position.y >= a.min.y && position.y <= a.max.y;
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