using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;
using System.Text.RegularExpressions;
using TMPro;


public class SSD300Detector : MonoBehaviour
{
    [SerializeField] private NNModel _modelFile;
    public TextAsset File;
    
    [SerializeField] private string INPUT_NAME;
    [SerializeField] private string OUTPUT_NAME;
    
    public const int IMAGE_SIZE = 224;
    private string[] labels;
    
    private IWorker _worker;

    private GameObject _camerCaptureObject;
    public TextMeshPro _textMesh;
   
    private void Start()
    {
        Debug.Log("Get reference to camera capture");
        
       _camerCaptureObject = GameObject.Find("Camera");
       
       var model = ModelLoader.Load(this._modelFile);
        
        Debug.Log("Model Loaded");
        
        Debug.Log("Worker Creation");
        
        Debug.Log("Graphics API: " + SystemInfo.graphicsDeviceType);

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
        {
            var workerType = WorkerFactory.Type.ComputePrecompiled; // GPU
            _worker = WorkerFactory.CreateWorker(workerType, model);
            
            Debug.Log("Created worker on GPU");
        }
        else
        {
            var workerType = WorkerFactory.Type.CSharpBurst;  // CPU
            _worker = WorkerFactory.CreateWorker(workerType, model);
            
            Debug.Log("Created worker on CPU");
        }
        
        this.labels = Regex.Split(this.File.text, "\n|\r|\r\n")
            .Where(s => !String.IsNullOrEmpty(s)).ToArray();
    }

    private int i = 0;
    public IEnumerator Detect(Color32[] image, bool version, System.Action<List<KeyValuePair<string, float>>> callback)
    {
        //_camerCaptureObject.SetActive(false);
        var map = new List<KeyValuePair<string, float>>();

        using (var tensor = TransformInput(image, IMAGE_SIZE, IMAGE_SIZE))
        {
            var input = new Dictionary<string, Tensor>();
            input.Add(INPUT_NAME, tensor);
            Debug.Log("Created Input: " + input);
            
            Debug.Log("Start Inference");
            
            var enumerator = this._worker.ExecuteAsync(input);

            Debug.Log("Inference terminated");

            while (enumerator.MoveNext())
            {
                i++;
                if (i >= 20)
                {
                    i = 0;
                    yield return null;
                }
            };
            
            var output = _worker.PeekOutput(OUTPUT_NAME);

            Debug.Log("Get Output: " + output);
            
            //if version is true use MobileNetV1, else use MobileNetV2
            if (version == true)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    map.Add(new KeyValuePair<string, float>(labels[i], output[i]*100));
                }
            }
            else
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    map.Add(new KeyValuePair<string, float>(labels[i], output[i]));
                }
            }
            
        }
        callback(map.OrderByDescending(x => x.Value).ToList());
        
        
    }

    public void Segment(Texture2D image)
    {
        _camerCaptureObject.SetActive(false);

        using (var tensor = TransformingInput(image, 480, 360))
        {
            var input = new Dictionary<string, Tensor>();
            input.Add(INPUT_NAME, tensor);
            Debug.Log("Created Input: " + input);

            Debug.Log("Start Inference");

            this._worker.Execute(input);

            Debug.Log("Inference terminated");
            
            var output = _worker.PeekOutput(OUTPUT_NAME);

            Debug.Log("Get Output: " + output);
            
            
            
        }

    }
    
    
    private static Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
        Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,false);
        float incX=(1.0f / (float)targetWidth);
        float incY=(1.0f / (float)targetHeight);
        for (int i = 0; i < result.height; ++i) {
            for (int j = 0; j < result.width; ++j) {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        result.Apply();
        return result;
    }
    
    
    public static Tensor TransformInput(Color32[] pic, int width, int height)
    {
        //Texture2D new_image = ScaleTexture(image, width, height);
        
        //Color32[] pic = new_image.GetPixels32();
        
        float[] floatValues = new float[width * height * 3];

        for (int i = 0; i < pic.Length; ++i)
        {
            var color = pic[i];

            floatValues[i * 3 + 0] = (color.r - 0) / 255.0f;
            floatValues[i * 3 + 1] = (color.g - 0) / 255.0f;
            floatValues[i * 3 + 2] = (color.b - 0) / 255.0f;
        }

        return new Tensor(1, height, width, 3, floatValues);
    }
    
    public static Tensor TransformingInput(Texture2D image, int width, int height)
    {
        Texture2D new_image = ScaleTexture(image, width, height);
        
        Color32[] pic = new_image.GetPixels32();
        
        float[] floatValues = new float[width * height * 3];

        for (int i = 0; i < pic.Length; ++i)
        {
            var color = pic[i];

            floatValues[i * 3 + 0] = (color.r - 0) / 255.0f;
            floatValues[i * 3 + 1] = (color.g - 0) / 255.0f;
            floatValues[i * 3 + 2] = (color.b - 0) / 255.0f;
        }

        return new Tensor(1, height, width, 3, floatValues);
    }
}
