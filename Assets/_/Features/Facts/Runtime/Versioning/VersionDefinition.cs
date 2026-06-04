using UnityEngine;

namespace TheFoundation.Runtime
{
    [CreateAssetMenu(
        fileName = "VersionDefinition",
        menuName = "TheFundation/Versioning/Version Definition")]
    public class VersionDefinition : ScriptableObject
    {
        [Header("Version du jeu")]
        public string m_versionString = "1.0.0";
    }
}