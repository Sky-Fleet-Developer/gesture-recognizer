using System;

namespace Gestures.Analysis
{
    public interface IMatcher
    {
        public event Action<AAMatch> OnMatchUpdated;
    }
}