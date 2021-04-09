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
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json;
using CustomVison;
using System;

public class ScanContext
{
    public double horizontalAngleRadian { get; protected set; }
    public float formFactor { get; protected set; }
    public Transform origin { get; protected set; }

    public ScanContext( double angle, float ratio, Transform cameraTransform)
    {
        horizontalAngleRadian = angle  * (Math.PI / 180); 
        formFactor = ratio;
        origin = cameraTransform;
    }
}


public class PhotoCamera : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;

    public GameObject PhotoPrefab;
    public GameObject cursor;
    public TextMeshPro info;
    public bool showPicture = true;
    public double horizontalAngle = 64.69f; // how much of the image do we take

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
    private ScanContext scanContext;
    private ObjectLabeler labeler;
    private CustomVisionResult result;
    private string debugText = "Debug";
    //private Dictionary<System.Guid, ScanContext> scanContextMap = new Dictionary<System.Guid, ScanContext>();
    // we may use a dictionary to store many scans waiting for results ... let's start with one for now

    // Use this for initialization
    void Start()
    {
        
        gaze = CoreServices.InputSystem.GazeProvider;
        cursor.SetActive(false);//disable cursor
        shutterSound = GetComponent<AudioSource>() as AudioSource;
        labeler = GetComponent<ObjectLabeler>() as ObjectLabeler; 
        mqttHelper = GetComponent<MqttHelper>() as MqttHelper;
        mqttHelper.Subscribe( ResultReceiver);

        // Debug.Log("File path " + Application.persistentDataPath);
        // take lower resolution available
        if ((PhotoCapture.SupportedResolutions != null) && (PhotoCapture.SupportedResolutions.Count() > 0))
        {
            cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();
            ratio = (float)cameraResolution.height / (float)cameraResolution.width;
        } else {
            ratio = 9f / 16f;
        }
        scanContext = new ScanContext(horizontalAngle, ratio, Camera.main.transform); // create a context with Camera position.
        Debug.Log("scanContext init " + scanContext.ToString());
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
        info.SetText(debugText);
        float dist;
        float focus = 0.0f;
        if (startPicture)
        {
            gazePoint = gaze.HitInfo.point;
            if (gazePoint != null)
            {

                if (cursor != null) { cursor.transform.position = gazePoint; }
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.Normalize();
                cursor.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
                if (gazeStarted == false)
                {
                    startPoint = gazePoint;
                    dist = Vector3.Distance(Camera.main.transform.position, gazePoint);
                    focus = dist * 0.02f;
                    cursor?.SetActive(true); //enable cursor
                    gazeStarted = true;
                    timer = 0.0f;
                    debugText += " ... test gaze started with focus  " + focus;
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
        
        if (result != null)
        {
            try
            {
                labeler.LabelObjects(result.recognitionData, scanContext.horizontalAngleRadian, scanContext.formFactor, scanContext.origin);
                debugText += "\nLabel Set " + result.ID;
            } catch(Exception e)
            {
                Debug.Log("label error " + e.Message); 
                debugText += e.Message;
            }
            // infotext = "Received message from " + e.Topic + " : " + msg;
            result = null;
        }
    }
    public void StopCamera()
    {
        // Deactivate our camera
        photoCaptureObject?.StopPhotoModeAsync(OnStoppedPhotoMode);
    }
    public void StartTakePicture()
    {
        Debug.Log("StartTakePicture() called");
        debugText += " ... start take picture";

        gazeStarted = false;
        startPicture = true;
    }
    public void TakePicture()
    {
        Debug.Log("TakePicture() called");
        debugText += " ... take picture";

        CameraParameters cameraParameters = new CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
        //cameraParameters.pixelFormat = showPicture == true ? CapturePixelFormat.BGRA32 : CapturePixelFormat.JPEG;
        
        scanContext = new ScanContext(horizontalAngle,ratio,Camera.main.transform); // create a context with Camera position.
        // Activate the camera
        if (photoCaptureObject != null)
        {
            if (shutterSound != null)
            {
                shutterSound.Play();
                debugText += " ... click";
                mqttHelper.Publish("debug", "click ... ");
            }
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                // Take a picture
                debugText += " ... Photo Async";
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        }
        else
        {
            Debug.Log("camera object is not defined");
            debugText += "camera object is not defined";

            //mqttHelper.Publish("image", "{\"ID\":\"" + 00 + "\",\"image\":\"" + "" + "\"}"); 
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        debugText += " ... picture in memory";
        mqttHelper.Publish("debug", " ... picture in memory");

        List<byte> imageBufferList = new List<byte>();

        byte[] imageArray;
        mqttHelper.Publish("debug", " ... checking Format");
        if (photoCaptureFrame.pixelFormat == CapturePixelFormat.JPEG)
        {
            mqttHelper.Publish("debug", " ... picture is JPEG");
            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
            imageArray = imageBufferList.ToArray();
        }
        else
        {
            mqttHelper.Publish("debug", " ... picture is RAW");
            // Copy the raw image data into our target texture
            imageArray = ConvertAndShowOnDebugPane(photoCaptureFrame);
        }

        mqttHelper.Publish("debug", " ... start convert");
        string data = System.Convert.ToBase64String(imageArray);
        string pictureID = System.Guid.NewGuid().ToString();
        mqttHelper.Publish("image", "{\"ID\":\""+pictureID+"\",\"image\":\""+data+"\"}");
        // save the camera position and image size


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
            mqttHelper.Publish("debug", " ... UploadImageDataToTexture");
            photoCaptureFrame.UploadImageDataToTexture(fullTexture);
            // crop a  portion of the image at the center
            double horizontalCameraAngleRadian = 64.69f * (Math.PI / 180); // Hololens2 camera angle in deg
            double angleRadian = horizontalAngle * (Math.PI / 180);
            double cropFactor = Math.Tan(angleRadian / 2f) / Math.Tan(horizontalCameraAngleRadian / 2f);
            
            int dx = (int)(cameraResolution.width * cropFactor);
            int dy = (int)(cameraResolution.height * cropFactor);
            int x = (cameraResolution.width - dx) / 2;
            int y = (cameraResolution.height - dy) / 2;
            Color[] pix = fullTexture.GetPixels(x, y, dx, dy);
            var targetTexture = new Texture2D(dx, dy);
            targetTexture.SetPixels(pix);
            mqttHelper.Publish("debug", " ... targetTexture.Apply");
            targetTexture.Apply();
            // Create a gameobject that we can apply our texture to

            mqttHelper.Publish("debug", " ... Instantiate PhotoPrefab");
            if (showPicture)
            {
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

                mqttHelper.Publish("debug", " ... check scan context");
                Vector3 cameraForward = scanContext.origin.forward;
                cameraForward.Normalize();
                var dist = 1.0f;
                newElement.transform.position = Camera.main.transform.position + (cameraForward * dist);

                mqttHelper.Publish("debug", " ... LookRotation");
                newElement.transform.rotation = Quaternion.LookRotation(cameraForward, scanContext.origin.up); // align with camera up 
                Vector3 scale = newElement.transform.localScale;
                scale.x = 2f * dist * (float)Math.Tan(angleRadian / 2f);
                scale.y = scale.x * ratio; // scale the entire photo on height
                newElement.transform.localScale = scale;
            }
            mqttHelper.Publish("debug", " ... EncodeToJPG");
            List<byte> raw = new List<byte>(targetTexture.EncodeToJPG());
            return raw.ToArray();

        }
        catch (System.Exception e)
        {
            info.SetText("error " + e.Message);
            mqttHelper.Publish("debug", " ... error: " + e.Message);
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
    private Transform CopyCameraTransForm()
    {
        var g = new GameObject();
        g.transform.position = Camera.main.transform.position;
        g.transform.rotation = Camera.main.transform.rotation;
        g.transform.localScale = Camera.main.transform.localScale;
        return g.transform;
    }
    public void ResultReceiver(string msg)
    {
        
        Debug.Log("Received message : " + msg);
        debugText += msg;
        try
        {
            result = JsonConvert.DeserializeObject<CustomVisionResult>(msg);
            debugText += "\n message ID " + result.ID;
        } catch (Exception e)
        {
            debugText += "Error " + e.Message;
        }
        
    }
}