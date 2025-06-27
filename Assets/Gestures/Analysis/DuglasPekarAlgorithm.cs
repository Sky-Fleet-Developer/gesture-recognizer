using System;
using System.Collections.Generic;
using Gestures.Utilities;
using UnityEngine;

namespace Gestures.Analysis
{
    public static class DuglasPekarAlgorithm
    {
        public static void MakeFragments(List<Vector2> points, float tolerance, List<int> fragments)
        {
            fragments.Add(0);
            fragments.Add(points.Count - 1);
            HashSet<int> closedFragments = new();
            int added = 1;
            int cycles = 0;
            while (added > 0)
            {
                added = 0;
                for (var frag = 1; frag < fragments.Count; frag++)
                {
                    if (cycles++ > 999999)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    int a = fragments[frag - 1];
                    int b = fragments[frag];
                    int hashCode = a | (b << 16);
                    if (closedFragments.Contains(hashCode)) // cancel checked fragments
                    {
                        continue;
                    }

                    int middle = Mathf.RoundToInt((b + a) * 0.5f);
                    int d = Mathf.Max(b - middle, middle - a);
                    bool cornerFound = false;
                    for (int i = 0; i < d; i++)
                    {
                        int index;
                        // positive check
                        index = middle + i;
                        if (index < b)
                        {
                            if (!CheckCorner(points, a, b, index, tolerance))
                            {
                                #if UNITY_EDITOR
                                Debug.DrawLine(points[a], points[index], Color.yellow * 0.1f, 5);
                                #endif
                                fragments.Insert(frag, index);
                                cornerFound = true;
                                added++;
                                break;
                            }
                        }

                        // negative check
                        if (i > 0)
                        {
                            index = middle - i;
                            if (index > a)
                            {
                                if (!CheckCorner(points, a, b, index, tolerance))
                                {
                                    #if UNITY_EDITOR
                                    Debug.DrawLine(points[a], points[index], Color.yellow * 0.1f, 5);
                                    #endif
                                    fragments.Insert(frag, index);
                                    cornerFound = true;
                                    added++;
                                    break;
                                }
                            }
                        }
                    }

                    if (!cornerFound)
                    {
                        closedFragments.Add(hashCode); // mark fragment as checked
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static bool CheckCorner(List<Vector2> points, int a, int b, int corner, float tolerance)
        {
            Vector2 pA = points[a];
            Vector2 pB = points[b];
            Vector2 pCorner = points[corner];
            Vector2 direction = (pB - pA).normalized;
            return pCorner.IsInsideRange(pA, 0, direction, tolerance, false);
        }
    }
}