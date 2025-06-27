using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gestures.Analysis
{
    public class SimplifyInput : MonoBehaviour, IVectorArrayProcessor
    {
        [SerializeField] private float minCornerAngle;
        [SerializeField] private float minEdgeLength;
        [SerializeField] private GameObject input;
        private IVectorArrayProcessor _input;
        private List<int> _processedEdges = new ();
        public event Action OnPointMoved;
        public event Action OnPointPlaced;
        public event Action OnInputComplete;

        public IEnumerable<Vector2> GetPoints()
        {
            foreach (int index in _processedEdges)
            {
                yield return _input.GetPoint(index);
            }
        }
        public Vector2 GetPoint(int idx) => _input.GetPoint(_processedEdges[idx]);
        public int GetPointsCount() => _processedEdges.Count;
        private Vector2 P(int i) => _input.GetPoint(i);
        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnInputComplete += Clear;
            _input.OnPointMoved += Move;
            _input.OnPointPlaced += Bake;
            _processedEdges.Add(0);
            _processedEdges.Add(1);
        }

        private void OnDestroy()
        {
            _input.OnInputComplete -= Clear;
            _input.OnPointPlaced -= Bake;
            _input.OnPointMoved -= Move;
        }
        
        private void Move()
        {
            _processedEdges[^1] = _input.GetPointsCount() - 1;
            OnPointMoved?.Invoke();
        }
        
        
        //_processedEdges[^1] - to move
        //_processedEdges[^2] - placed
        // from processedEdges[^2]+1 to _input.GetPointsCount() - range to process
        private void Bake()
        {
            int startIdx = _processedEdges[^2];
            Vector2 previous = _processedEdges.Count > 2 ? P(_processedEdges[^3]) : P(_processedEdges[^2]) - P(_processedEdges[^1]);
            Vector2 current = P(startIdx);
            Vector2 intermediate = current;
            
            for (int i = startIdx + 1; i < _input.GetPointsCount(); i++)
            {
                Vector2 next = P(i);
                float angle = Vector2.SignedAngle(current - previous, next - intermediate);
                if (Mathf.Abs(angle) > minCornerAngle && Vector2.SqrMagnitude(P(_processedEdges[^2]) - intermediate) >= minEdgeLength * minEdgeLength)
                {
                    _processedEdges[^1] = i-1;
                    OnPointPlaced?.Invoke();
                    _processedEdges.Add(i-1);
                    previous = current;
                    current = next;
                }

                intermediate = next;
            }
            
            _processedEdges[^1] = _input.GetPointsCount() - 1;
            OnPointMoved?.Invoke();
        }

        private void Clear()
        {
            OnInputComplete?.Invoke();
            _processedEdges.Clear();
            _processedEdges.Add(0);
            _processedEdges.Add(1);
        }
    }
}