using Gestures.Analysis;
using UnityEngine;

namespace Gestures.View
{
    public class LogRecognizedMatch : MonoBehaviour
    {
        [SerializeField, Tooltip("Required IMatcher interface")] private GameObject matcher;
        [SerializeField, Tooltip("Required IVectorArrayProcessor interface")] private GameObject input;
        private IMatcher _matcher;
        private IVectorArrayProcessor _input;
        private AAMatch _match;

        private void Awake()
        {
            _matcher = matcher.GetComponent<IMatcher>();
            _matcher.OnMatchUpdated += OnMatchUpdated;
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnInputComplete += OnInputComplete;
        }

        private void OnDestroy()
        {
            _matcher.OnMatchUpdated -= OnMatchUpdated;
            _input.OnInputComplete -= OnInputComplete;
        }

        private void OnMatchUpdated(AAMatch match)
        {
            _match = match;
        }
        
        private void OnInputComplete()
        {
            if (!_match.IsNull && !_match.IsPartialMatch)
            {
                Debug.Log($"Recognized gesture \"{_match.GestureId}\"");
            }
        }
    }
}