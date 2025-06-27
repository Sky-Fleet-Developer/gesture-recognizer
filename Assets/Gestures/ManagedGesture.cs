using System;
using UnityEngine;

namespace Gestures
{
    [Serializable]
    public class ManagedGesture : Gesture
    {
        [SerializeField] private string name;
        public string Name => name;
    }
}