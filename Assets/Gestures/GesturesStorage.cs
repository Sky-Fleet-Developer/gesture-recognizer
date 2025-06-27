using System.Collections.Generic;
using UnityEngine;

namespace Gestures
{
    [CreateAssetMenu]
    public class GesturesStorage : ScriptableObject
    {
        [SerializeField] private List<ManagedGesture> gestures;
        public IEnumerable<ManagedGesture> GetGestures() => gestures;
    }
}