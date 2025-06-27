using System;
using System.Collections.Generic;
using Gestures.Utilities;
using UnityEngine;

namespace Gestures.Analysis
{
    public static class LangAlgorithm
    {
        public static void MakeFragments(List<Vector2> points, float tolerance, List<int> fragments)
        {
            fragments.Add(0);
            int startPointer = 0;
            while (startPointer < points.Count - 3)
            {
                bool hasMatch = false;
                for (int endPointer = startPointer + 2; endPointer < points.Count; endPointer++)
                {
                    if (!IsAllPointsInRange(points, startPointer + 1, endPointer - 1, tolerance))
                    {
#if UNITY_EDITOR
                        Debug.DrawLine(points[fragments[^1]], points[endPointer - 1], Color.yellow, 5);
#endif
                        fragments.Add(endPointer - 1);
                        startPointer = endPointer - 1;
                        hasMatch = true;
                        break;
                    }
                }

                if (!hasMatch)
                {
                    break;
                }
            }
#if UNITY_EDITOR
            Debug.DrawLine(points[fragments[^1]], points[^1], Color.yellow, 5);
#endif
            fragments.Add(points.Count - 1);
        }

        private static bool IsAllPointsInRange(List<Vector2> points, int startSeek, int endSeek, float tolerance)
        {
            Vector2 origin = points[startSeek - 1];
            Vector2 delta = points[endSeek + 1] - origin;

            float deltaLen = delta.magnitude;
            Vector2 direction = delta / deltaLen;
            for (int i = startSeek; i < endSeek; i++)
            {
                if (!points[i].IsInsideRange(origin, deltaLen, direction, tolerance, true))
                {
                    return false;
                }
            }

            return true;
        }
    }
}