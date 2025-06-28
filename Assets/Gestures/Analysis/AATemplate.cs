using System.Collections.Generic;
using Gestures.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gestures.Analysis
{
    public class AATemplate
    {
        public string Name { get; }
        public bool IsPartOfWhole { get; }
        private float _commonAngle;
        private float _commonLength;
        private List<float> _angles;
        private List<float> _lengths;
        private Vector2 _startOrientation;
            
        public AATemplate(string name, List<Vector2> points, float tolerance, bool isPartOfWhole)
        {
            Name = name;
            IsPartOfWhole = isPartOfWhole;
            points.Normalize();
            MakeAnalysis(points, tolerance);
        }

        private void MakeAnalysis(List<Vector2> points, float tolerance)
        {
            List<int> fragments = new();
            DuglasPekarAlgorithm.MakeFragments(points, tolerance, fragments);
            //LangAlgorithm.MakeFragments(points, tolerance, fragments);
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
                #if UNITY_EDITOR
                Debug.DrawLine(points[fragments[i - 1]], points[fragments[i]], Color.yellow, 5);
                #endif
                _lengths.Add((points[fragments[i - 1]] - points[fragments[i]]).magnitude);
                _commonLength += _lengths[^1];
            }
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
                if (i < _angles.Count)
                {
                    angle += _angles[i];
                }
            }
            return 0;
        }

        private const int PathComparisonSamplesCount = 10;
        public float CompareTo(AATemplate other)
        {
            float commonLengthComparison = Mathf.Min(other._commonLength, _commonLength) / Mathf.Max(other._commonLength, _commonLength);
            float commonAngleComparison = Mathf.Max(1 - Mathf.Abs(other._commonAngle - _commonAngle) / 180f, 0);

            float pathComparison = 1;
            float step = 1f / PathComparisonSamplesCount;
            for (int i = 0; i < PathComparisonSamplesCount; i++)
            {
                float myDir = GetDirectionByPathPercent(step * i);
                float otherDir = other.GetDirectionByPathPercent(step * i);

                pathComparison *= 0.4f + Mathf.Max(1 - Mathf.Abs(otherDir - myDir) / 180f, 0) * 0.6f;
            }

            float orientationComparison = Mathf.Pow(Mathf.Max(0, Vector2.Dot(_startOrientation, other._startOrientation)), 2);
                
            return commonLengthComparison * commonAngleComparison * pathComparison * orientationComparison;
        }
    }
}