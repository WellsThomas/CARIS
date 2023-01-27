using UnityEngine;

namespace Packages
{
    public class Utility
    {
        public static Vector3 CalculateThirdPoint(Vector3 point1, Vector3 point2, Vector3 position)
        {
            var p1 = ConvertTo2D(point1);
            var p2 = ConvertTo2D(point2);
            var diff = p1 - p2;
            var p3 = ConvertTo2D(position);
            var p4 = new Vector2(-diff.y, diff.x) + p2;
            var vec = GetIntersectionPointCoordinates(p2, p4, p3, diff + p3, out _);

            return new Vector3(vec.x, point1.y, vec.y);
        }
        
        public static Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found)
        {
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
 
            if (tmp == 0)
            {
                // No solution!
                found = false;
                return Vector2.zero;
            }
 
            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
 
            found = true;
 
            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }
        
        public static Vector2 ConvertTo2D(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
    }
}