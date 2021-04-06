using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.WebCam;
using TMPro;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Drawing;


public class PhotoCamera : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;

    public GameObject PhotoPrefab;
    public GameObject cursor;
    public TextMeshPro info;
    public bool showPicture = true;

    Resolution cameraResolution;

    float ratio = 1.0f;
    AudioSource shutterSound;
    private MqttHelper mqttHelper;
    private IMixedRealityGazeProvider gaze;
    private bool startPicture = false;
    private Vector3 startPoint;
    private Vector3 gazePoint;
    private bool gazeStarted = false;
    private float timer;

    // Use this for initialization
    void Start()
    {
        gaze = CoreServices.InputSystem.GazeProvider;
        cursor.SetActive(false);//disable cursor
        shutterSound = GetComponent<AudioSource>() as AudioSource;
        mqttHelper = GetComponent<MqttHelper>() as MqttHelper;
        Debug.Log("File path " + Application.persistentDataPath);
        // take lower resolution available
        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();


        ratio = (float)cameraResolution.height / (float)cameraResolution.width;

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            Debug.Log("camera ready to take picture");
        });
        PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOn);
    }
    void Update()
    {
        float dist;
        float focus = 0.0f;
        if (startPicture)
        {
            gazePoint = gaze.HitInfo.point;
            if (gazePoint != null)
            {

                cursor.transform.position = gazePoint;
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.Normalize();


                cursor.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
                if (gazeStarted == false)
                {
                    startPoint = gazePoint;
                    dist = Vector3.Distance(Camera.main.transform.position, gazePoint);
                    focus = dist * 0.02f;
                    cursor.SetActive(true); //enable cursor
                    gazeStarted = true;
                    timer = 0.0f;
                    info.SetText("test gaze started with focus  " + focus);
                }
                else
                {
                    // if (Vector3.Distance(startPoint, gazePoint) > focus)
                    // {
                    //     // gaze moving away 
                    //     info.SetText("test gaze reset " + Vector3.Distance(startPoint, gazePoint));
                    //    startPoint = gazePoint;
                    //     timer = 0.0f;
                    //     
                    // }
                    // else
                    // {
                    timer += Time.deltaTime; // add frame duration
                    if (timer > 1.5f)
                    {
                        Debug.Log("test gaze at same point done -> take picture  ");
                        startPicture = false; // 1 second of gaze at same point 
                        gazeStarted = false;
                        cursor.SetActive(false);

                        TakePicture();
                    }
                    // }
                }
            }
            else
            {
                cursor.SetActive(false);
                if (gazeStarted == true)
                {
                    gazeStarted = false;

                }

            }

        }

    }
    public void StopCamera()
    {
        // Deactivate our camera

        photoCaptureObject?.StopPhotoModeAsync(OnStoppedPhotoMode);
    }
    public void StartTakePicture()
    {
        gazeStarted = false;
        startPicture = true;
    }
    public void TakePicture()
    {
        CameraParameters cameraParameters = new CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        //cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
        cameraParameters.pixelFormat = showPicture == true ? CapturePixelFormat.BGRA32 : CapturePixelFormat.JPEG;

        // Activate the camera
        if (photoCaptureObject != null)
        {
            if (shutterSound != null)
            {
                shutterSound.Play();
            }
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        }
        else
        {
            info.SetText("camera object is not defined");
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        List<byte> imageBufferList = new List<byte>();

        byte[] imageArray;
        if (photoCaptureFrame.pixelFormat == CapturePixelFormat.JPEG)
        {

            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
            imageArray = imageBufferList.ToArray();
        }
        else
        {
            // Copy the raw image data into our target texture
            imageArray = ConvertAndShowOnDebugPane(photoCaptureFrame);

        }



        string data = System.Convert.ToBase64String(imageBufferList.ToArray());
        mqttHelper.Publish("image", data);

        //You may only use this method if you specified the BGRA32 format in your CameraParameters.
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

    }
    private byte[] ConvertAndShowOnDebugPane(PhotoCaptureFrame photoCaptureFrame)
    {
        // get image in byte array and crop it

        //  List<byte> raw = new List<byte>(targetTexture.EncodeToJPG());

        try
        {

            var fullTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            //targetTexture.LoadRawTextureData(imageBufferList.ToArray()); // use memory stream array to create the texture
            photoCaptureFrame.UploadImageDataToTexture(fullTexture);
            int x = cameraResolution.width / 3;
            int y = cameraResolution.height / 3;
            int dx = cameraResolution.width / 3;
            int dy = cameraResolution.height / 3;
            Color[] pix = fullTexture.GetPixels(x, y, dx, dy);
            var targetTexture = new Texture2D(dx, dy);
            targetTexture.SetPixels(pix);
            targetTexture.Apply();
            // Create a gameobject that we can apply our texture to

            GameObject newElement = Instantiate<GameObject>(PhotoPrefab);
            GameObject quad = newElement.transform.Find("Quad").gameObject;
            Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
            quadRenderer.material.mainTexture = targetTexture;
            // new Material(Shader.Find("Unlit/Texture"));

            // Set position and rotation 
            // Bug in Hololens v2 and Unity 2019 about PhotoCaptureFrame not having the location data - March 2020
            // 
            // Matrix4x4 cameraToWorldMatrix;
            // photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            //  Vector3 position = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            //  Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
            // Vector3 cameraForward = cameraToWorldMatrix * Vector3.forward;






            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.Normalize();
            newElement.transform.position = Camera.main.transform.position + (cameraForward * 1.0f);

            newElement.transform.rotation = Quaternion.LookRotation(cameraForward, Camera.main.transform.up); // align with camera up 
            Vector3 scale = newElement.transform.localScale;
            scale.y = scale.y * ratio; // scale the entire photo on height
            newElement.transform.localScale = scale;
            List<byte> raw = new List<byte>(targetTexture.EncodeToJPG());
            return raw.ToArray();

        }
        catch (System.Exception e)
        {
            info.SetText("error " + e.Message);
            return null;
        }

        //return raw.ToArray();
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown our photo capture resource
        //photoCaptureObject.Dispose();
        //photoCaptureObject = null;
    }
}