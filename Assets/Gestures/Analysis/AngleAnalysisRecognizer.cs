using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gestures.Analysis
{
    public class AngleAnalysisRecognizer : MonoBehaviour, IMatcher
    {
        [SerializeField] private GameObject input;
        [SerializeField] private GesturesStorage storage;
        [SerializeField] private float tolerance;
        [SerializeField] private float minProb;
        
        public event Action<AAMatch> OnMatchUpdated;
        private AAMatch _currentMatch;
        
        
        private IVectorArrayProcessor _input;
        private List<AATemplate> _sourceTemplates;
        private List<Vector2> _store = new();
        private List<Result> _results = new();

        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnPointPlaced += Process;
            _sourceTemplates = new ();
            foreach (var managedGesture in storage.GetGestures())
            {
                for (int i = 0; i < managedGesture.GetLongElementsCount() - 1; i++)
                {
                    managedGesture.Evaluate(_store, i+1, 6);
                    _sourceTemplates.Add(new AATemplate($"{managedGesture.Name}_{i+1}", _store, tolerance, true));
                }
                managedGesture.Evaluate(_store, 6);
                _sourceTemplates.Add(new AATemplate($"{managedGesture.Name}_full", _store, tolerance, false));
            }
        }

        private void Process()
        {
            _store.Clear();
            foreach (var vector2 in _input.GetPoints())
            {
                _store.Add(vector2);
            }

            _results.Clear(); // multiple results were need to make tests
            AATemplate templateToCompare = new AATemplate("Comparable", _store, tolerance, false);
            foreach (var result in _sourceTemplates.Select(x => new Result(templateToCompare, x))
                         .Where(x => x.Prob > minProb)
                         .OrderBy(x => x.TemplateB.IsPartOfWhole ? -x.Prob + 1 : -x.Prob))
                         
            {
                _results.Add(result);
            }

            if (_results.Count == 0)
            {
                if (!_currentMatch.IsNull)
                {
                    _currentMatch = AAMatch.Null;
                    OnMatchUpdated?.Invoke(_currentMatch);
                }
            }
            else
            {
                _currentMatch = new AAMatch
                {
                    GestureId = _results[0].TemplateB.Name,
                    IsPartialMatch = _results[0].TemplateB.IsPartOfWhole,
                    Prob = _results[0].Prob
                };
                OnMatchUpdated?.Invoke(_currentMatch);
            }

            /*for (int i = 0; i < Mathf.Min(_results.Count, 3); i++)
            {
                Debug.Log($"Match_{i}: {_results[i].TemplateB.Name} by {_results[i].Prob} percents");
            }*/
        }
        
        private class Result : IComparable<Result>
        {
            public AATemplate TemplateA { get; }
            public AATemplate TemplateB { get; }
            public float Prob { get; }

            internal Result(AATemplate templateA, AATemplate templateB)
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
    }
}