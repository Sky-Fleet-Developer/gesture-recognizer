using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gestures.Analysis
{
    public class AngleAnalysisRecognizer : MonoBehaviour
    {
        [SerializeField] private GameObject input;
        [SerializeField] private GesturesStorage storage;
        [SerializeField] private float tolerance;
        
        private IVectorArrayProcessor _input;
        private List<AATemplate> _sourceTemplates;
        private List<Vector2> _store = new();

        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnPointPlaced += Process;
            _sourceTemplates = new ();
            foreach (var managedGesture in storage.GetGestures())
            {
                managedGesture.Evaluate(_store, 6);
                _sourceTemplates.Add(new AATemplate(managedGesture.Name, _store, tolerance));
            }
        }

        private void Process()
        {
            _store.Clear();
            foreach (var vector2 in _input.GetPoints())
            {
                _store.Add(vector2);
            }

            AATemplate templateToCompare = new AATemplate("Comparable", _store, tolerance);
            Result result = _sourceTemplates.Select(x => new Result(templateToCompare, x)).OrderBy(x => -x.Prob).First();
            Debug.Log($"Best match: {result.TemplateB.Name} by {result.Prob} percents");
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