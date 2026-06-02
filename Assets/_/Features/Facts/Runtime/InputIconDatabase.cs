using System.Collections.Generic;
using UnityEngine;

namespace TheFundation.Runtime
{
    [CreateAssetMenu(fileName="InputIconDatabase", menuName="Input/Icon Database")]
    public class InputIconDatabase : ScriptableObject
    {
        public List<SchemeSet> sets = new();

        [System.Serializable] public class SchemeSet
        {
            public string scheme; // "KeyboardMouse", "XboxController", etc.
            public List<ActionIcon> icons = new();
            public List<ActionText> labels = new();
            public List<ActionSprite> sprites = new();
        }

        [System.Serializable] public class ActionIcon { public string action; public Sprite sprite; }
        [System.Serializable] public class ActionText { public string action; public string label; }
        [System.Serializable] public class ActionSprite { public string action; public string spriteName; }

        public bool TryGet(string scheme, out SchemeSet set)
        {
            set = sets.Find(s => s.scheme == scheme);
            return set != null;
        }
    }
}