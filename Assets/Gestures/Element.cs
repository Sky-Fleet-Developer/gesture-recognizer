using System;
using UnityEngine;

namespace Gestures
{
    [Serializable]
    public class Element
    {
        public const float CurveRadianStep = Mathf.PI / 6;
        [SerializeField] private ElementType type;
        [SerializeField] private float value;

        public float GetLength()
        {
            switch (type)
            {
                case ElementType.StraightLine:
                    return value;
                case ElementType.Curve:
                    return value * CurveRadianStep;
                case ElementType.Corner:
                    return 0f;
                default:
                    return 0f;
            }
        }

        public Vector2 Evaluate(float percent)
        {
            switch (type)
            {
                case ElementType.StraightLine:
                    return Vector2.right * value * percent;
                case ElementType.Curve:
                    return new Vector2(Mathf.Sin(percent * CurveRadianStep) * value, Mathf.Cos(percent * CurveRadianStep) * value) - Vector2.up * value;
                case ElementType.Corner:
                    return Vector2.zero;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Vector2 GetOutputDirection()
        {
            switch (type)
            {
                case ElementType.StraightLine:
                    return Vector2.right;
                case ElementType.Curve:
                    return new Vector2(Mathf.Cos(CurveRadianStep), -Mathf.Sin(CurveRadianStep));
                case ElementType.Corner:
                    return new Vector2(Mathf.Cos(value * Mathf.Deg2Rad), -Mathf.Sin(value * Mathf.Deg2Rad));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}