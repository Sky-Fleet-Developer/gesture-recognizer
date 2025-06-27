using System.Collections.Generic;
using UnityEngine;

namespace Gestures.Analysis
{
    public class GestureRecognizerProcessor : MonoBehaviour
    {
        [SerializeField] private GameObject input;
        [SerializeField] private GesturesStorage storage;
        private ContinuousGestureRecognizer _continuousGestureRecognizer;
        
        private IVectorArrayProcessor _input;

        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnPointPlaced += Process;
            List<ContinuousGestureRecognizer.Template> templates = new ();
            List<Vector2> store = new List<Vector2>();
            foreach (var managedGesture in storage.GetGestures())
            {
                List<ContinuousGestureRecognizer.Pt> points = new ();

                managedGesture.Evaluate(store, 6);
                foreach (var vector2 in store)
                {
                    points.Add(new ContinuousGestureRecognizer.Pt(vector2));
                }
                templates.Add(new ContinuousGestureRecognizer.Template(managedGesture.Name, points));
            }

            _continuousGestureRecognizer = new ContinuousGestureRecognizer(templates, 1);
        }

        private int c = 0;
        private void Process()
        {
           // if (c++ % 10 == 0)
            {
                List<ContinuousGestureRecognizer.Pt> points = new ();
                foreach (var vector2 in _input.GetPoints())
                {
                    points.Add(new ContinuousGestureRecognizer.Pt(vector2));
                }

                var results = _continuousGestureRecognizer.Recognize(points);
                Debug.Log($"Recognized as {results[0].Template.Id}, in p = {results[0].Prob}");

            }
        }
    }
}