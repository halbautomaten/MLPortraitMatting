#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Copies necessary DLLs and ONNX models to their respective folders after building the project.
    /// </summary>
    public class PostBuildProcessor : MonoBehaviour
    {
        [PostProcessBuildAttribute()]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
        {
            var package = "com.halbautomaten.backgroundsegmentation";
            var plugins = "Plugins";
            var models = "Models";
            var dll = "DirectML.dll";
            var architecture = "x86_64";
            var onnx = "*.onnx";

            var source = Path.GetFullPath(Path.Combine("Packages", package));
            var build = Path.GetDirectoryName(pathToBuildProject);

            // Copy DirectML.dll
            var dllDst = Path.Combine(build, dll);
            if (!File.Exists(dllDst))
            {
                var dllSrc = Path.Combine(source, plugins, architecture, dll);
                try
                {
                    File.Copy(dllSrc, dllDst);
                    Debug.Log($"Copied {dllSrc} to {dllDst}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not copy DLL {dllSrc} to {dllDst}: {ex.Message}");
                }
            }

            // Create ONNX models folder
            var onnxDst = Path.Combine(build, models);
            if (!Directory.Exists(onnxDst))
            {
                Directory.CreateDirectory(onnxDst);
                Debug.Log($"Created directory {onnxDst}");
            }

            // Remove existing ONNX models from previous build
            var removeModels = Directory.GetFiles(Path.Combine(build, models), onnx);
            foreach (var file in removeModels)
            {
                File.Delete(file);
            }

            // Copy ONNX models
            List<string> files = new();
            foreach (UnityEngine.Object go in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)) as UnityEngine.Object[])
            {
                BgSegModelInfo modelInfo = go as BgSegModelInfo;
                if (modelInfo != null)
                {
                    Debug.Log($"Found reference in scene to ONNX model: {modelInfo.OnnxModelName}");
                    var path = Path.Combine(source, models, modelInfo.OnnxModelName);
                    files.Add(path);
                }
            }
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var modelDst = Path.Combine(onnxDst, name);
                if (!File.Exists(modelDst))
                {
                    try
                    {
                        File.Copy(file, modelDst);
                        Debug.Log($"Copied {file} to {modelDst}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Could not copy ONNX model {file} to {modelDst}: {ex.Message}");
                    }
                }
            }
        }
    }
}
#endif
