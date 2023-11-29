namespace GameEngine.Util.Values;

public struct Rect
{

    public Vector2<float> Position = new();
    public Vector2<float> Size = new();

    public float X
    {
        get {return Position.X;}
        set {Position.X = value;}
    }
    public float Y
    {
        get {return Position.Y;}
        set {Position.Y = value;}
    }
    public float Width
    {
        get {return Size.X;}
        set {Size.X = value;}
    }
    public float Height
    {
        get {return Size.Y;}
        set {Size.Y = value;}
    }

    public Rect() {}
    public Rect(Vector2<float> position, Vector2<float> size)
    {
        Position = position;
        Size = size;
    }
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    // TODO intersection calculations

}