namespace Gestures.Analysis
{
    public struct AAMatch
    {
        public string GestureId;
        public bool IsPartialMatch;
        public float Prob;

        public bool IsNull => string.IsNullOrEmpty(GestureId);

        public static AAMatch Null => new AAMatch() { GestureId = null };
    }
}