using UnityEditor;


namespace TheFoundation.Editor
{

    [InitializeOnLoad]
    public static class GitHooksSetup
    {
        static GitHooksSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        private static void Setup()
        {
            var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "config core.hooksPath .githooks",
                WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),
                UseShellExecute = false
            });
            result?.WaitForExit();
        }
    }
}