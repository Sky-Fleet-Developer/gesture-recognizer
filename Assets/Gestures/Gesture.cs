using System;
using System.Collections.Generic;
using System.Linq;
using Gestures.Utilities;
using UnityEngine;

namespace Gestures
{
    [Serializable]
    public class Gesture
    {
        [SerializeField] private List<Element> elements;
        [SerializeField] private int startDirection; // 0 is right, 3 is up, 6 is left, 9 is down

        public int GetLongElementsCount() => elements.Count(x => x.GetLength() > 1e-5);
        
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
            Evaluate(store, GetLongElementsCount(), frequency);
        }
        public void Evaluate(List<Vector2> store, int overrideElementsCount, int frequency)
        {
            store.Clear();
            store.Add(Vector2.zero);
            Vector2 direction = GetStartDirection();
            Vector2 position = Vector2.zero;
            float step = 1f / frequency;
            for (var e = 0; e < elements.Count; e++)
            {
                var element = elements[e];
                if (element.GetLength() > 1e-5)
                {
                    if (overrideElementsCount == 0)
                    {
                        return;
                    }
                    overrideElementsCount--;
                    for (int i = 1; i < frequency; i++)
                    {
                        Vector2 localPoint = element.Evaluate(step * i).Rotate(direction);
                        #if UNITY_EDITOR
                        Debug.DrawLine(store[^1], position + localPoint, Color.red, 5);
                        #endif
                        store.Add(position + localPoint);
                    }

                    Vector2 offset = element.Evaluate(1).Rotate(direction);
                    #if UNITY_EDITOR
                    Debug.DrawLine(store[^1], position + offset, Color.red, 5);
                    #endif
                    position += offset;
                }

                store.Add(position);
                direction = direction.Rotate(element.GetOutputDirection());
            }
        }
    }
}