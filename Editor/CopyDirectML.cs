#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Tries to copy DirectML.dll to the same location as the Unity Editor executable
    /// </summary>
    [InitializeOnLoad]
    public class CopyDirectML
    {
        static CopyDirectML()
        {
            Process();
        }

        private static void Process()
        {
            var dll = "DirectML.dll";
            var architecture = "x86_64";
            var editor = Directory.GetParent(EditorApplication.applicationPath).ToString();
            var root = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(root, dll, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.Contains(architecture))
                {
                    var target = Path.Combine(editor, dll);
                    if (!File.Exists(target))
                    {
                        try
                        {
                            File.Copy(file, target);
                            Debug.Log($"Copied {file} to {target}");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Debug.LogWarning($"Could not copy a required DLL from {file} to {target}. You need to copy this manually.");
                            // FIXME: handle permission denied error
                        }
                    }
                }
            }
        }
    }
}
#endif