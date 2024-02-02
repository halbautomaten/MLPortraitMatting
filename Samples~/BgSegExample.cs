using System.Collections;
using UnityEngine;
using halbautomaten.BackgroundSegmentation;

public class BgSegExample : MonoBehaviour
{
    public RenderTexture inputTexture;
    public RenderTexture imageTexture;
    public RenderTexture alphaTexture;
    public BgSegModelInfo mattingModel;
    public BgSegModelInfo depthModel;
    public bool webcam = true;
    WebCamTexture webcamTexture;
    BgSegController bgSegController = new();

    void Start()
    {
        if (webcam)
        {
            webcamTexture = new WebCamTexture();
            webcamTexture.Play();
        }

        bgSegController.Initialize(1280, 720);
        bgSegController.LoadModel(mattingModel, 0);
        bgSegController.LoadModel(depthModel, 0);

        StartCoroutine(InternalUpdate());
    }

    void OnDestroy()
    {
        bgSegController.CleanUp();
    }

    IEnumerator InternalUpdate()
    {
        while (Application.isPlaying)
        {
            if (webcam)
                Graphics.Blit(webcamTexture, inputTexture);

            yield return bgSegController.ProcessImage(inputTexture, imageTexture, alphaTexture);
        }
    }
}
