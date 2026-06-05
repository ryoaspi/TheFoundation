using System.Collections.Generic;
using UnityEngine;

namespace TheFoundation.Editor
{
    [CreateAssetMenu(fileName = "ProjectNote", menuName = "TheFoundation/Project Note Definition")]
    public class ProjectNoteDefinition : ScriptableObject
    {
        public string m_ID;
        public string m_title;
        public ProjectNoteType m_category;
        [TextArea(4, 12)]
        public string m_description;
        public string m_version;
        public string m_author;
        public string m_updatedAt;
        public List<Texture2D> m_gallery = new();
    }
}