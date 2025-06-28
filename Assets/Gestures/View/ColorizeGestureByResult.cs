using System;
using Gestures.Analysis;
using UnityEngine;

namespace Gestures.View
{
    public class ColorizeGestureByResult : MonoBehaviour
    {
        [SerializeField, Tooltip("Required IMatcher interface")] private GameObject matcher;
        [SerializeField] private GestureView view;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color mismatchColor = Color.red;
        private IMatcher _matcher;

        private void Awake()
        {
            _matcher = matcher.GetComponent<IMatcher>();
            _matcher.OnMatchUpdated += OnMatchUpdated;
        }

        private void OnDestroy()
        {
            _matcher.OnMatchUpdated -= OnMatchUpdated;
        }

        private void OnMatchUpdated(AAMatch match)
        {
            view.BrushColor = match.IsNull ? mismatchColor : normalColor;
            view.SetBrushState(!match.IsNull);
        }
    }
}