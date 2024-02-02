using UnityEngine;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Asset for configuring a specific ONNX model
    /// </summary>
    [CreateAssetMenu(menuName = "halbautomaten/Model Info")]
    public class BgSegModelInfo : ScriptableObject
    {
        [Tooltip("Filename of the ONNX model as it appears in the models folder")]
        public string OnnxModelName = "model_file.onnx";

        [Tooltip("Type of the model class as defined in BgSegModelInterface (must match model Id in DLL)")]
        public BgSegModelInterface.Models OnnxModelType = BgSegModelInterface.Models.UNKNOWN;

        [Tooltip("The width of the input tensor (Use ONNX tools to read this info from the .onnx model)")]
        public int modelTensorWidth = 512;

        [Tooltip("The height of the input tensor (Use ONNX tools to read this info from the .onnx model)")]
        public int modelTensorHeight = 512;
    }
}
