using System.Collections.Generic;
using Packages.Serializable;
using UnityEngine;

public class Line
{
    public int id;
    public readonly List<LineSegment> Segments;
    public readonly List<GameObject> LineObjects;
    public int Width;
    public Color Color;
    public int DrawingId;
    private LineSegment _currentLineSegment;

    public Line(int id, int width, Color color, int drawingId)
    {
        this.id = id;
        this.DrawingId = drawingId;
        Segments = new List<LineSegment>();
        LineObjects = new List<GameObject>();
        Width = width;
        Color = color;
    }
    
    public void AddToLine(GameObject newLineObject, Vector2 from, Vector2 to)
    {
        // Assess line-segment situation
        if (Segments.Count == 0 || _currentLineSegment.length == LineSegment.MaxSize)
        {
            _currentLineSegment = new LineSegment(id, Width, Color, DrawingId, from);
            Segments.Add(_currentLineSegment);
        }
        
        // Add to segment
        _currentLineSegment.points[_currentLineSegment.length] = to;
        _currentLineSegment.length++;

        // Add to current line
        LineObjects.Add(newLineObject);
    }

    public bool IsLineSegmentFinished()
    {
        return Segments.Count == 0 || _currentLineSegment.length == LineSegment.MaxSize;
    }

    public LineSegment GetCurrentLineSegment()
    {
        return _currentLineSegment;
    }
}