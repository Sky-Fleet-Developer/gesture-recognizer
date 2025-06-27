using System;
using System.Collections.Generic;
using Gestures.Utilities;
using UnityEngine;

namespace Gestures
{
    [Serializable]
    public class Gesture
    {
        [SerializeField] private List<Element> elements;
        [SerializeField] private int startDirection; // 0 is right, 3 is up, 6 is left, 9 is down

        public Vector2 GetStartDirection()
        {
            return new Vector2(Mathf.Cos(startDirection * Element.CurveRadianStep), Mathf.Sin(startDirection * Element.CurveRadianStep));
        }

        public float GetLength()
        {
            float result = 0;
            foreach (var element in elements)
            {
                result += element.GetLength();
            }
            return result;
        }

        public void Evaluate(List<Vector2> store, int frequency)
        {
            store.Clear();
            store.Add(Vector2.zero);
            Vector2 direction = GetStartDirection();
            Vector2 position = Vector2.zero;
            float step = 1f / frequency;
            foreach (var element in elements)
            {
                if (element.GetLength() > 0)
                {
                    for (int i = 1; i < frequency; i++)
                    {
                        Vector2 localPoint = element.Evaluate(step * i).Rotate(direction);
                        Debug.DrawLine(store[^1], position + localPoint, Color.red, 5);
                        store.Add(position + localPoint);
                    }
                    Vector2 offset = element.Evaluate(1).Rotate(direction);
                    Debug.DrawLine(store[^1], position + offset, Color.red, 5);
                    position += offset;
                }
                store.Add(position);
                direction = direction.Rotate(element.GetOutputDirection());
            }
        }
    }
}