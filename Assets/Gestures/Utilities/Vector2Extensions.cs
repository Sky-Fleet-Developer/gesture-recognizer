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
    }
}