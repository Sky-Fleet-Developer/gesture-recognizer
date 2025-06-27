using System.Collections.Generic;
using UnityEngine;

namespace Gestures.Utilities
{
    public static class Vector2Extensions
    {
        public static Vector2 Rotate(this Vector2 vector, Vector2 matrix)
        {
            return new Vector2(vector.x * matrix.x - vector.y * matrix.y, vector.x * matrix.y + vector.y * matrix.x);
        }
        public static Vector2 RotateInv(this Vector2 vector, Vector2 matrix)
        {
            return new Vector2(vector.x * matrix.x + vector.y * matrix.y, -vector.x * matrix.y + vector.y * matrix.x);
        }
        
        public static bool IsInsideRange(this Vector2 point, Vector2 start, float edgeLength, Vector2 direction, float tolerance, bool testByLongitudeBorders)
        {
            Vector2 testVector = (point - start).RotateInv(direction);
            bool longitudeBorders = !testByLongitudeBorders || (testVector.x > 0 && testVector.x < edgeLength);
            return longitudeBorders && Mathf.Abs(testVector.y) < tolerance;
        }
        
        public static void Normalize(this List<Vector2> points)
        {
            Rect aabb = new Rect(points[0], Vector2.zero);
            foreach (var point in points)
            {
                aabb.min = Vector2.Min(aabb.min, point);
                aabb.max = Vector2.Max(aabb.max, point);
            }

            float maxSide = Mathf.Max(aabb.size.y, aabb.size.x);
            aabb.size = Vector2.one * maxSide;
            
            for (var i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                point -= aabb.center;
                point.x *= 100 / aabb.size.x;
                point.y *= 100 / aabb.size.y;
                points[i] = point;
            }
        }
    }
}