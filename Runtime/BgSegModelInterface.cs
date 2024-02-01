using System;
using System.Runtime.InteropServices;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Interface to functions in the plugin DLL
    /// </summary>
    public static class BgSegModelInterface
    {
        const string dll = "BgSegUnityOnnxPlugin";

        public enum Models
        {
            MODNET,         // plugin model id = 0
            MIDAS,          // plugin model id = 1
            PP_HUMANSEG,     // plugin model id = 2
            UNKNOWN
        };

        [DllImport(dll)]
        public static extern IntPtr LastError();

        [DllImport(dll)]
        public static extern int GetNumProviders();

        [DllImport(dll)]
        public static extern IntPtr GetProviderName(int index);

        [DllImport(dll)]
        public static extern void CleanUp();

        [DllImport(dll)]
        public static extern void InitONNXRuntime();

        [DllImport(dll)]
        public static extern bool LoadModel(int modelId, string modelPath, string executionProvider, int tensorWidth, int tensorHeight);

        [DllImport(dll)]
        public static extern bool RunModel(int modelId, IntPtr input, int[] inputDim, IntPtr output, int[] outputDim);
    }
}
