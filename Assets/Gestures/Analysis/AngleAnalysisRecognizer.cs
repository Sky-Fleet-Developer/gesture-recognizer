using System;
using System.Collections.Generic;
using System.Linq;
using Gestures.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gestures.Analysis
{
    public class AngleAnalysisRecognizer : MonoBehaviour
    {
        [SerializeField] private GameObject input;
        [SerializeField] private GesturesStorage storage;
        [SerializeField] private float tolerance;
        [SerializeField] private Template t;
        private ContinuousGestureRecognizer _continuousGestureRecognizer;
        
        private IVectorArrayProcessor _input;
       [SerializeField] private List<Template> _sourceTemplates; 
        List<Vector2> store = new();

        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnPointPlaced += Process;
            _sourceTemplates = new ();
            foreach (var managedGesture in storage.GetGestures())
            {
                managedGesture.Evaluate(store, 6);
                _sourceTemplates.Add(new Template(managedGesture.Name, store, tolerance));
            }
        }

        private void Process()
        {
            store.Clear();
            foreach (var vector2 in _input.GetPoints())
            {
                store.Add(vector2);
            }

            t = new Template("Comparable", store, tolerance);
            Result result = _sourceTemplates.Select(x => new Result(t, x)).OrderBy(x => -x.Prob).First();
            Debug.Log($"Best match: {result.TemplateB.Name} by {result.Prob} percents");

            /*List<Result> results = _sourceTemplates.Select(x => new Result(t, x)).OrderBy(x => -x.Prob).ToList();

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                Debug.Log($"{i}th place: {result.TemplateB.Name} by {result.Prob} percents");
            }*/
        }
        
        private class Result : IComparable<Result>
        {
            public Template TemplateA { get; }
            public Template TemplateB { get; }
            public float Prob { get; }

            internal Result(Template templateA, Template templateB)
            {
                TemplateA = templateA;
                TemplateB = templateB;

                Prob = TemplateA.CompareTo(templateB);
            }

            public int CompareTo(Result other)
            {
                if (Prob == other.Prob) return 0;
                return Prob < other.Prob ? 1 : -1;
            }
        }
        
        [Serializable]
        private class Template
        {
            public string Name { get; }
            [SerializeField]private float _commonAngle;
            [SerializeField]private float _commonLength;
            [SerializeField]private List<float> _angles;
            [SerializeField]private List<float> _lengths;
            [SerializeField] private Vector2 _startOrientation;
            
            public Template(string name, List<Vector2> points, float tolerance)
            {
                Name = name;
                Normalize(points);
                MakeAnalysis(points, tolerance);
            }

            private void MakeAnalysis(List<Vector2> points, float tolerance)
            {
                List<int> fragments = new();
                DuglasPekarAlgorithm(points, tolerance, fragments);
                //LangAlgorithm(points, tolerance, fragments);
                _startOrientation = (points[fragments[1]] - points[fragments[0]]).normalized;

                _angles = new();
                for (var i = 2; i < fragments.Count; i++)
                {
                    Vector2 a = points[fragments[i - 2]];
                    Vector2 b = points[fragments[i - 1]];
                    Vector2 c = points[fragments[i]];
                    
                    _angles.Add(Vector2.SignedAngle(b - a, c - b));
                    _commonAngle += _angles[^1];
                }

                _lengths = new();
                for (var i = 1; i < fragments.Count; i++)
                {
                    Debug.DrawLine(points[fragments[i - 1]], points[fragments[i]], Color.yellow, 5);
                    _lengths.Add((points[fragments[i - 1]] - points[fragments[i]]).magnitude);
                    _commonLength += _lengths[^1];
                }
            }

            private void DuglasPekarAlgorithm(List<Vector2> points, float tolerance, List<int> fragments)
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
                                if (!DuglasPekarCheck(points, a, b, index, tolerance))
                                {
                                    Debug.DrawLine(points[a], points[index], Color.yellow * 0.1f, 5);
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
                                    if (!DuglasPekarCheck(points, a, b, index, tolerance))
                                    {
                                        Debug.DrawLine(points[a], points[index], Color.yellow * 0.1f, 5);
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

            private bool DuglasPekarCheck(List<Vector2> points, int a, int b, int corner, float tolerance)
            {
                Vector2 pA = points[a];
                Vector2 pB = points[b];
                Vector2 pCorner = points[corner];
                Vector2 direction = (pB - pA).normalized;
                return IsPointInsideRange(pA, 0, direction, pCorner, tolerance, false);
            }

            private void LangAlgorithm(List<Vector2> points, float tolerance, List<int> fragments)
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
                            Debug.DrawLine(points[fragments[^1]], points[endPointer-1], Color.yellow, 5);
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
                                
                Debug.DrawLine(points[fragments[^1]], points[^1], Color.yellow, 5);
                fragments.Add(points.Count - 1);
            }

            private bool IsAllPointsInRange(List<Vector2> points, int startSeek, int endSeek, float tolerance)
            {
                Vector2 origin = points[startSeek - 1];
                try
                {
                    Vector2 delta = points[endSeek + 1] - origin;

                float deltaLen = delta.magnitude;
                Vector2 direction = delta / deltaLen;
                for (int i = startSeek; i < endSeek; i++)
                {
                    if (!IsPointInsideRange(origin, deltaLen, direction, points[i], tolerance, true))
                    {
                        return false;
                    }
                }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                return true;
            }
            
            
            private bool IsPointInsideRange(Vector2 start, float edgeLength, Vector2 direction, Vector2 point, float tolerance, bool testByLongitudeBorders)
            {
                Vector2 testVector = (point - start).RotateInv(direction);
                bool longitudeBorders = !testByLongitudeBorders || (testVector.x > 0 && testVector.x < edgeLength);
                return longitudeBorders && Mathf.Abs(testVector.y) < tolerance;
            }

            private float GetDirectionByPathPercent(float pathPercent)
            {
                Assert.IsFalse(pathPercent > 1);
                float percent = 0;
                float angle = 0;
                for (int i = 0; i < _lengths.Count; i++)
                {
                    float fragmentLength = _lengths[i] / _commonLength;
                    if (percent + fragmentLength > pathPercent)
                    {
                        return angle;
                    }

                    percent += fragmentLength;
                    angle += _angles[i]; // out of range possible only when pathPercent greater than one;
                }
                return 0;
            }

            private const int PathComparisonSamplesCount = 10;
            public float CompareTo(Template other)
            {
                float commonLengthComparison = Mathf.Min(other._commonLength, _commonLength) / Mathf.Max(other._commonLength, _commonLength);
                float commonAngleComparison = Mathf.Max(1 - Mathf.Abs(other._commonAngle - _commonAngle) / 360f, 0);

                float pathComparison = 1;
                float step = 1f / PathComparisonSamplesCount;
                for (int i = 0; i < PathComparisonSamplesCount; i++)
                {
                    float myDir = GetDirectionByPathPercent(step * i);
                    float otherDir = other.GetDirectionByPathPercent(step * i);

                    pathComparison *= 0.5f + Mathf.Max(1 - Mathf.Abs(otherDir - myDir) / 360f, 0) * 0.5f;
                }

                float orientationComparison = Vector2.Dot(_startOrientation, other._startOrientation);
                
                return commonLengthComparison * commonAngleComparison * pathComparison * orientationComparison;
            }
        }
        
        private static void Normalize(List<Vector2> points)
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