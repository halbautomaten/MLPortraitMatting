#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Adds the compositor shader to the Always Included Shaders list in ProjectSettings/GraphicsSettings 
    /// after the package was installed
    /// </summary>
    public class PostPackageInstallProcessor : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        static void SubscribeToEvent()
        {
            Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            AddShaderToAlwaysIncluded("BackgroundSegmentation/Compositor");
        }

        public static void AddShaderToAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                return;

            var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var serializedObj = new SerializedObject(graphicsSettings);
            var arrayProp = serializedObj.FindProperty("m_AlwaysIncludedShaders");
            bool hasShader = false;
            for (int i = 0; i < arrayProp.arraySize; ++i)
            {
                var arrayElem = arrayProp.GetArrayElementAtIndex(i);
                if (shader == arrayElem.objectReferenceValue)
                {
                    hasShader = true;
                    Debug.Log($"Shader \'{shaderName}\' already included in ProjectSettings/GraphicsSettings/AlwaysIncludedShaders");
                    break;
                }
            }

            if (!hasShader)
            {
                Debug.Log($"Adding shader \'{shaderName}\' to ProjectSettings/GraphicsSettings/AlwaysIncludedShaders");

                int arrayIndex = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(arrayIndex);
                var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
                arrayElem.objectReferenceValue = shader;

                serializedObj.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
