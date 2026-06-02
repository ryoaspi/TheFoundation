using System.Collections.Generic;
using UnityEngine;

namespace TheFundation.Runtime
{
    public class GameHealthReport
    {
        public readonly List<string> m_errors = new();
        public readonly List<string> m_warnings = new();

        public bool HasErrors => m_errors.Count > 0;
        public bool HasWarnings => m_warnings.Count > 0;

        public void AddError(string msg)
        {
            m_errors.Add(msg);
            Debug.LogError("[GameHealth] ERROR → " + msg);
        }

        public void AddWarning(string msg)
        {
            m_warnings.Add(msg);
            Debug.LogWarning("[GameHealth] WARNING → " + msg);
        }
    }
}