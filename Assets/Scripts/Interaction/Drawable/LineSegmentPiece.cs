using UnityEngine;

namespace Interaction.Drawable
{
    public struct LineSegmentPiece
    {
        public Vector2 A, B;
        public int Width;
        public Line Line;
        public Color Color;

        public LineSegmentPiece(Vector2 a, Vector2 b, int width, Line line, Color color)
        {
            this.A = a;
            this.B = b;
            this.Width = width;
            this.Line = line;
            this.Color = color;
        }
    }
}