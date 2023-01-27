using Packages.Serializable;
using UnityEngine;

[System.Serializable]
public class LineSegment
{
    public int id;
    public Vector2[] points;
    public int length;
    public SerializableColor color;
    public int width;
    public int drawingId;
    public static readonly int MaxSize = 10;

    public LineSegment(int id, int width, Color color, int drawingId, Vector2 initialPoint)
    {
        this.id = id;
        this.width = width;
        this.color = color;
        this.drawingId = drawingId;
        
        // Setup length and points
        points = new Vector2[MaxSize];
        points[0] = initialPoint;
        length = 1;
    }
}