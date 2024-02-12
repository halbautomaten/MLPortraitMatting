using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace halbautomaten.BackgroundSegmentation
{
    /// <summary>
    /// Main logic for  background segmentation package.
    /// </summary>
    public class BgSegController
    {
        List<string> executionProviders = new();
        List<string> modelNames = new();
        List<string> modelPaths = new();

        byte[] inputData = { 0 };
        byte[] outputData = { 0 };
        int[] inputDim = { 0, 0 };
        int[] outputDim = { 0, 0 };

        bool isInitialized = false;
        List<BgSegModelInfo> model = new();
        Texture2D inputTexture, outputTexture;
        List<Texture2D> modelResultTexture = new();
        Material compositorMaterial;
        int processPhase = 0;
        bool haveMattingTex = false;

        float depthEdge0 = 0.35f;
        float depthEdge1 = 0.65f;

        public float DepthEdge0 { set => depthEdge0 = value; }
        public float DepthEdge1 { set => depthEdge1 = value; }

        public bool EnableLogging = false;

        string ModelFolder
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath("Packages/com.halbautomaten.backgroundsegmentation/Models");
#else
                return Path.GetFullPath("Models");
#endif
            }
        }

        /// <summary>
        /// Get a reference to the internal model output texture
        /// </summary>
        /// <param name="index">The index of the loaded model using this texture</param>
        /// <returns></returns>
        public Texture2D GetTexture(int index)
        {
            return modelResultTexture[index];
        }

        /// <summary>
        /// Initialize the ONNX runtime, collect available execution providers and initialize internal working textures
        /// </summary>
        /// <param name="width">Expected width of input texture</param>
        /// <param name="height">Expected height of input texture</param>
        public void Initialize(int width, int height)
        {
            isInitialized = false;
            BgSegModelInterface.InitONNXRuntime();
            CollectExecutionProviders();

            // Create internal working textures
            InitTexture(ref inputTexture, width, height, TextureFormat.RGB24);
            InitTexture(ref outputTexture, width, height, TextureFormat.RGB24);

            var shader = Shader.Find("BackgroundSegmentation/Compositor");
            if (shader == null)
            {
                Debug.LogError("[BackgroundSegmentation] Could not find compositor shader! Using default shader, which only shows the input image.");
                shader = Shader.Find("Unlit/Texture");
            }
            compositorMaterial = new Material(shader);

            isInitialized = true;
        }

        public void CleanUp()
        {
            BgSegModelInterface.CleanUp();
            isInitialized = false;
        }

        /// <summary>
        /// Get the list of available ONNX runtime execution providers. Must be called after Initialize().
        /// </summary>
        /// <returns></returns>
        public List<string> GetExecutionProviders()
        {
            return executionProviders;
        }

        /// <summary>
        /// Load an ONNX model and initialize corresponding internal result texture 
        /// </summary>
        /// <param name="modelInfo">Configuration of the ONNX model to load</param>
        /// <param name="executionProviderIndex">Execution provider to use for inference</param>
        /// <returns></returns>
        public bool LoadModel(BgSegModelInfo modelInfo, int executionProviderIndex)
        {
            bool success = false;

            if (!isInitialized)
            {
                Debug.LogError($"Must call Initialize() before loading a model");
                return false;
            }

            if (executionProviderIndex < executionProviders.Count)
            {
                var path = Path.Combine(ModelFolder, modelInfo.OnnxModelName);
                var result = BgSegModelInterface.LoadModel((int)modelInfo.OnnxModelType, path, executionProviders[executionProviderIndex], modelInfo.modelTensorWidth, modelInfo.modelTensorHeight);
                if (!result)
                {
                    IntPtr error = BgSegModelInterface.LastError();
                    var msg = Marshal.PtrToStringAnsi(error);
                    Debug.LogError($"While loading ONNX model: {msg}");
                }
                else
                {
                    // ... prepare anything else?
                    // ... inform UI?
                    
                    if(EnableLogging)
                        Debug.Log($"Successfully loaded new ONNX model {modelInfo.OnnxModelName} running on {executionProviders[executionProviderIndex]}");

                    // Add model to list
                    model.Add(modelInfo);

                    // Create model result texture
                    var tex = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.R8, false, false);
                    modelResultTexture.Add(tex);
                    success = true;
                }
            }
            else
            {
                Debug.LogError($"Unsupported execution provider selected: {executionProviderIndex}");
            }

            return success;
        }

        /// <summary>
        /// Run the processing pipeline. Runs inference with all loaded models. 
        /// </summary>
        /// <param name="webcamTexture">The current input image from web camera live stream</param>
        /// <param name="imageTexture">The input image used for each inference within one pipeline run (this is in sync with the output mask image)</param>
        /// <param name="maskTexture">The resulting alpha mask</param>
        /// <returns></returns>
        public IEnumerator ProcessImage(RenderTexture webcamTexture, RenderTexture imageTexture, RenderTexture maskTexture)
        {
            if (webcamTexture != null && maskTexture != null)
            {
                if (processPhase == 0)
                {
                    // Get input and output dimensions
                    inputDim[0] = webcamTexture.width;
                    inputDim[1] = webcamTexture.height;
                    outputDim[0] = maskTexture.width;
                    outputDim[1] = maskTexture.height;

                    // Adjust output buffer
                    var outputSize = outputDim[0] * outputDim[1]; // alpha only
                    if (outputData.Length != outputSize)
                    {
                        outputData = new byte[outputSize];
                    }

                    // Download image texture data from GPU to CPU
                    AsyncGPUReadback.Request(webcamTexture, 0, inputTexture.format, OnReadbackComplete); // copies to inputTexture

                    // Get texture data of input image
                    inputData = inputTexture.GetRawTextureData(); // TODO: Check garbage collection

                    // Advance to next processing phase
                    processPhase++;
                }

                if (processPhase >= 1 && processPhase <= model.Count)
                {
                    // Debug.Log($"Processing {processPhase}");

                    // Select active model based on process phase
                    var modelIndex = processPhase - 1;

                    // Process each loaded models
                    PerformInference((int)model[modelIndex].OnnxModelType, inputData, inputDim, outputData, outputDim);

                    // Adjust internal working textures and copy result
                    var tex = modelResultTexture[modelIndex];
                    InitTexture(ref tex, inputDim[0], inputDim[1], TextureFormat.R8);
                    tex.LoadRawTextureData(outputData);
                    tex.Apply();

                    // Advance to next processing phase
                    processPhase++;
                }

                if (model.Count == 0 || processPhase > model.Count)
                {
                    if (imageTexture != null)
                    {
                        // Debug.Log("Compositing");

                        // Rewrite input used for processing to keep in sync with resulting alpha mask
                        outputTexture.LoadRawTextureData(inputData);
                        outputTexture.Apply();
                        Graphics.Blit(outputTexture, imageTexture);

                        // Compute alpha mask via shader
                        // TODO: work out suitable compositor shader that uses the model outputs to create an alpha mask

                        // Set original input as foreground
                        compositorMaterial.SetTexture("_MainTex", imageTexture);

                        // Set matting and depth textures if their models are loaded
                        haveMattingTex = false;
                        for (int i = 0; i < modelResultTexture.Count; i++)
                        {
                            AssignTexToShaderProperty(i);
                        }

                        // Update depth threshold (only needed when depth model is loaded)
                        compositorMaterial.SetFloat("_DepthEdge0", depthEdge0);
                        compositorMaterial.SetFloat("_DepthEdge1", depthEdge1);

                        // Apply shader to generate alpha mask texture
                        Graphics.Blit(imageTexture, maskTexture, compositorMaterial);
                    }

                    // Rewind processing phases
                    processPhase = 0;
                }
            }
            yield return null;
        }

        //Call DLL interface method with ponters to image data
        // As it pins memory, this method must be marked as unsafe
        unsafe void PerformInference(int modelId, byte[] inputData, int[] inputDim, byte[] outputData, int[] outputDim)
        {
            // Pin memory
            fixed (byte* p = inputData, q = outputData)
            {
                var ret = BgSegModelInterface.RunModel(modelId, (IntPtr)p, inputDim, (IntPtr)q, outputDim);
                if (!ret)
                {
                    IntPtr error = BgSegModelInterface.LastError();
                    var msg = Marshal.PtrToStringAnsi(error);
                    Debug.LogError($"Error during inference: {msg}");
                }
            }
        }

        // Build a list of available ONNX models found in the plugin's model folder
        void CollectONNXModels()
        {
            modelPaths.Clear();
            modelNames.Clear();

            var root = ModelFolder;
            var files = Directory.GetFiles(root, "*.onnx");
            
            if(EnableLogging)
                Debug.Log($"Found {files.Length} ONNX models");
            
            foreach (var file in files)
            {
                modelPaths.Add(file);
                var path = file.Split('\\');
                var filename = path[path.Length - 1].Split('.')[0];
                var name = filename.Split('_')[0];
                modelNames.Add(name);
                Debug.Log($"\t{name}");
            }
        }

        // Build a list of available ONNX execution providers
        // Calls plugin function
        void CollectExecutionProviders()
        {
            executionProviders.Clear();

            int count = BgSegModelInterface.GetNumProviders();
            
            if(EnableLogging)
                Debug.Log($"Found {count} ONNX execution providers");

            for (var i = 0; i < count; i++)
            {
                var provider = Marshal.PtrToStringAnsi(BgSegModelInterface.GetProviderName(i));
                provider = provider.Replace("ExecutionProvider", "");
                executionProviders.Add(provider);
                Debug.Log($"\t{provider}");
            }
        }

        // Initialize a texture with given params
        void InitTexture(ref Texture2D tex, int width, int height, TextureFormat format)
        {
            if (tex == null)
            {
                // tex = new Texture2D(width, height, format, false, true);
                tex = new Texture2D(width, height, format, false, false);
            }
            else if (tex.width != width || tex.height != height)
            {
                tex.Reinitialize(width, height);
            }
        }

        // Callback when copying a texture from GPU to CPU has finished
        void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU read-back failed.");
                return;
            }

            // Might be null last couple of frames on exit
            if (inputTexture)
            {
                inputTexture.LoadRawTextureData(request.GetData<uint>());
                inputTexture.Apply();
            }
        }

        // Determine whether model provides matting information
        bool IsMattingModel(BgSegModelInfo info)
        {
            return info.OnnxModelType == BgSegModelInterface.Models.MODNET ||
                info.OnnxModelType == BgSegModelInterface.Models.PP_HUMANSEG;

        }

        // Determine whether model type provides depth information
        bool IsDepthModel(BgSegModelInfo info)
        {
            return info.OnnxModelType == BgSegModelInterface.Models.MIDAS;
        }

        // Assign the selected model result texture to a matching shader property
        // Distinguished between matting and depth model type
        // Currently, up to two matting models are supported by the compositor shader
        // If one matting texture was already assigned, then the next matting texture will be assigned to the second slot
        void AssignTexToShaderProperty(int index)
        {
            if (compositorMaterial != null && modelResultTexture != null && modelResultTexture.Count > index)
            {
                if (IsMattingModel(model[index]))
                {
                    if (!haveMattingTex)
                    {
                        compositorMaterial.SetTexture("_MattingTex", modelResultTexture[index]);
                        haveMattingTex = true;
                    }
                    else
                    {
                        compositorMaterial.SetTexture("_Matting2Tex", modelResultTexture[index]);
                    }
                }
                else if (IsDepthModel(model[index]))
                {
                    compositorMaterial.SetTexture("_DepthTex", modelResultTexture[index]);
                }
            }
        }
    }
}
