using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    [InitializeOnLoad]
    public class ProjectNoteBootstrap : MonoBehaviour
    {
        static ProjectNoteBootstrap() => EditorApplication.delayCall += Check;

        private static void Check()
        {
            var unseen = ProjectNoteDatabase.LoadAll()
                .Where(pn => !string.IsNullOrEmpty(pn.m_ID) && !ProjectNoteRegistry.IsSeen(pn.m_ID))
                .ToList();

            if (unseen.Count > 0)
                ProjectNotePopupWindow.Show(unseen);
        }
    }
}
