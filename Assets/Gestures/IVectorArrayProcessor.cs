using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gestures
{
    public interface IVectorArrayProcessor
    {
        public event Action OnPointMoved;
        public event Action OnPointPlaced;
        public event Action OnInputComplete;
        public IEnumerable<Vector2> GetPoints();
        public Vector2 GetPoint(int idx);
        public int GetPointsCount();
    }
}