using System;
using System.Collections;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CameraCapture : MonoBehaviour
{

    [SerializeField] private ARCameraManager _CameraManager;

    XRCpuImage.Transformation _Transformation = XRCpuImage.Transformation.MirrorY;

    private SSD300Detector _SSD300Detector;
    Texture2D _CameraTexture;
    private bool isWorking = false;
    public Text Text;
    
    //if mode is true make Classification, else Segmentation
    //change model on Unity
    public bool mode;
    
    //if version is true use MobileNetV1, else use MobileNetV2
    //change model on Unity
    public bool version;
    
    private void OnEnable()
    {
        if (_CameraManager != null)
        {
            _CameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    private void OnDisable()
    {
        if (_CameraManager != null)
        {
            _CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void Start()
    {
        _SSD300Detector = GameObject.Find("Detector").GetComponent<SSD300Detector>();
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        UpdateCameraImage();
    }

    unsafe void UpdateCameraImage()
    {
        if (!_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            Debug.Log("Latest Image not acquired");
            return;
        }

        var format = TextureFormat.RGBA32;

        if (_CameraTexture == null || _CameraTexture.width != image.width || _CameraTexture.height != image.height)
        {
            _CameraTexture = new Texture2D(image.width, image.height, format, false);

        }

        var conversionParams = new XRCpuImage.ConversionParams(image, format, _Transformation);

        var rawTextureData = _CameraTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            Debug.Log("Image Acquired");
        }
        finally
        {
            image.Dispose();
            Debug.Log("Image disposed");
        }

        _CameraTexture.Apply();

        if(mode == true)
        {
            Classify(_CameraTexture);
        }
        else
        {
            _SSD300Detector.Segment(_CameraTexture);
        }
            
        //_SSD300Detector.Segment(_CameraTexture);
    }


    private void Classify(Texture2D texture)
    {
        if (this.isWorking)
        {
            return;
        }

        this.isWorking = true;
        StartCoroutine(ProcessImage(texture, SSD300Detector.IMAGE_SIZE, result =>
        {
            StartCoroutine(_SSD300Detector.Detect(result, version, probabilities =>
            {
                //this.Text.text = String.Empty;

                if (probabilities.Any())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Debug.Log("Key--------" + probabilities[i].Key + " Value-------" + String.Format("{0:0.000}%", probabilities[i].Value));
                        this.Text.text = probabilities[i].Key + ": " + String.Format("{0:0.000}%", probabilities[i].Value) + "\n";
                     
                    }
                }

                Resources.UnloadUnusedAssets();
                this.isWorking = false;
            }));
        }));
    }

    private IEnumerator ProcessImage(Texture2D texture, int inputSize, System.Action<Color32[]> callback)
    {
        yield return StartCoroutine(TextureTools.CropSquare(texture,
            TextureTools.RectOptions.Center, snap =>
            {
                var scaled = Scale(snap, inputSize);
                var rotated = Rotate(scaled.GetPixels32(), scaled.width, scaled.height);
                callback(rotated);
            }));

    }


    private Texture2D Scale(Texture2D texture, int imageSize)
    {
        var scaled = TextureTools.scaled(texture, imageSize, imageSize, FilterMode.Bilinear);

        return scaled;
    }


    private Color32[] Rotate(Color32[] pixels, int width, int height)
    {
        return TextureTools.RotateImageMatrix(
            pixels, width, height, -90);
    }
}





