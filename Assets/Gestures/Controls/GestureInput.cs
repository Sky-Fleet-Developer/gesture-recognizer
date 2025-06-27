using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gestures.Controls
{
    public class GestureInput : MonoBehaviour, IVectorArrayProcessor, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] private float minElementsDistance;
        private List<Vector2> _points = new ();
        private RectTransform _rtr;
        public event Action OnPointMoved;
        public event Action OnPointPlaced;
        public event Action OnInputComplete;

        public IEnumerable<Vector2> GetPoints() => _points;
        public Vector2 GetPoint(int idx) => _points[idx];
        public int GetPointsCount() => _points.Count;

        private void Awake()
        {
            _rtr = transform as RectTransform;
        }

        private Vector2 GetPointInRect(Vector2 input)
        {
            return (Vector2)_rtr.InverseTransformPoint(input) + _rtr.rect.size * 0.5f;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            Vector2 p = GetPointInRect(eventData.position);
            _points[^1] = p;
            if (TryAddNewPoint(p))
            {
                OnPointPlaced?.Invoke();
                _points.Add(p);
            }
            else
            {
                OnPointMoved?.Invoke();
            }
        }

        private bool TryAddNewPoint(Vector2 position)
        {
            Vector2 prevPoint = _points[^2];
            if (Vector2.SqrMagnitude(prevPoint - position) > minElementsDistance * minElementsDistance)
            {
                return true;
            }
            return false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 point = GetPointInRect(eventData.position);
            _points.Add(point);
            _points.Add(point);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _points.Add(GetPointInRect(eventData.position));
            OnPointPlaced?.Invoke();
            OnInputComplete?.Invoke();
            _points.Clear();
        }
    }
}