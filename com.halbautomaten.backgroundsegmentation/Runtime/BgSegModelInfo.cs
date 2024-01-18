using UnityEngine;

namespace halbautomaten.BackgroundSegmentation
{
    [CreateAssetMenu(menuName = "halbautomaten/Model Info")]
    public class BgSegModelInfo : ScriptableObject
    {
        public string OnnxModelName = "model_file.onnx";
        public BgSegModelInterface.Models OnnxModelType = BgSegModelInterface.Models.UNKNOWN;
    }
}
