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
        origin = CopyTransform(cameraTransform);
    }
    private Transform CopyTransform(Transform t)
    {
        var g = new GameObject();
        g.transform.position = t.position;
        g.transform.rotation = t.rotation;
        g.transform.localScale = t.localScale;
        return g.transform;
    }
}


public class PhotoCamera : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;

    public GameObject PhotoPrefab;
    public GameObject ScannerScreen;
    public GameObject cursor;
    public TextMeshPro info;
    public bool showPicture = true;
    public double horizontalAngle = 64.69f; // how much of the image do we take
    public string outboundTopic = "hololensimage";
    public float scannerScreenDistance = 1.0f;
    Resolution cameraResolution;

    float ratio = 1.0f;
    
    AudioSource shutterSound;
    private double angleRadian;
    const double horizontalCameraAngleRadian = 64.69f * (Math.PI / 180); // Hololens2 camera angle in deg
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
        angleRadian = horizontalAngle * (Math.PI / 180);
        gaze = CoreServices.InputSystem.GazeProvider;
        cursor?.SetActive(false);//disable cursor
        
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
            Debug.Log("Resolution " + cameraResolution.height + " x " + cameraResolution.width);
        } else
        {
            ratio = 9f / 16f;
        }
        scanContext = new ScanContext(horizontalAngle, ratio, Camera.main.transform); // create a context with Camera position.
        ScannerScreen.SetActive(false);  // remove the scanner
        Vector3 scale = ScannerScreen.transform.localScale;
        scale.x = 2f * scannerScreenDistance * (float)Math.Tan(angleRadian / 2f);
        scale.y = scale.x * ratio; // scale the entire photo on height
        ScannerScreen.transform.localScale = scale;

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
                placeScanner(Camera.main.transform);

                //if (cursor != null)
                //{
                //    Vector3 cameraForward = Camera.main.transform.forward; 
                //    cursor.transform.position = gazePoint - 0.1f* cameraForward;
                    
                //    cameraForward.Normalize();
                //    cursor.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
                //}
                if (gazeStarted == false)
                {
                    startPoint = gazePoint;
                    dist = Vector3.Distance(Camera.main.transform.position, gazePoint);
                    focus = dist * 0.02f;
                    cursor?.SetActive(true); //enable cursor
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
                        cursor?.SetActive(false);

                        TakePicture();
                    }
                    // }
                }
            }
            else
            {
                cursor.SetActive(false);
               // ScannerScreen.SetActive(false);
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
                ScannerScreen.GetComponent<MoveLine>().stopScanAnimation();
                
                ScannerScreen.SetActive(false);  // remove the scanner
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
        gazeStarted = false;
        startPicture = true;
    }
    private void TakePicture()
    {

        ;
        //cameraParameters.pixelFormat = showPicture == true ? CapturePixelFormat.BGRA32 : CapturePixelFormat.JPEG;

        scanContext = new ScanContext(horizontalAngle, ratio, Camera.main.transform); // create a context with Camera position.
        placeScanner(scanContext.origin);
        ScannerScreen.GetComponent<MoveLine>().startScanAnimation();
        
        StartCoroutine(TakePictureInternal());
        
    }
    private IEnumerator  TakePictureInternal()
    {
        
        CameraParameters cameraParameters = new CameraParameters();
        cameraParameters.hologramOpacity = 0.0f;
        cameraParameters.cameraResolutionWidth = cameraResolution.width;
        cameraParameters.cameraResolutionHeight = cameraResolution.height;
        cameraParameters.pixelFormat = CapturePixelFormat.BGRA32; 
        //cameraParameters.pixelFormat = showPicture == true ? CapturePixelFormat.BGRA32 : CapturePixelFormat.JPEG;
        
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
        yield return null;
    }
    void placeScanner(Transform origin)
    {
        ScannerScreen.SetActive(true);
        Vector3 cameraForward = origin.forward;
        var dist = 1.0f;
        ScannerScreen.transform.position = origin.position + (cameraForward * dist);

        ScannerScreen.transform.rotation = Quaternion.LookRotation(cameraForward, origin.up); // align with camera up 
        
        
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



        string data = System.Convert.ToBase64String(imageArray);
        string pictureID = System.Guid.NewGuid().ToString();
        mqttHelper.Publish(outboundTopic, "{\"ID\":\""+pictureID+"\",\"image\":\""+data+"\"}");
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
            photoCaptureFrame.UploadImageDataToTexture(fullTexture);
            // crop a  portion of the image at the center
            
            
            double cropFactor = Math.Tan(angleRadian / 2f) / Math.Tan(horizontalCameraAngleRadian / 2f);
            
            int dx = (int)(cameraResolution.width * cropFactor);
            int dy = (int)(cameraResolution.height * cropFactor);
            int x = (cameraResolution.width - dx)/ 2;
            int y = (cameraResolution.height - dy) / 2;
            Color[] pix = fullTexture.GetPixels(x, y, dx, dy);
            var targetTexture = new Texture2D(dx, dy);
            targetTexture.SetPixels(pix);
            targetTexture.Apply();
            // Create a gameobject that we can apply our texture to
            if (showPicture == true)
            {
                GameObject newElement = Instantiate<GameObject>(PhotoPrefab);
                GameObject quad = newElement.transform.Find("Quad").gameObject;
                Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
                quadRenderer.material.mainTexture = targetTexture;

                Vector3 cameraForward = scanContext.origin.forward;
                cameraForward.Normalize();
                var dist = 1.0f;
                newElement.transform.position = scanContext.origin.position + (cameraForward * dist);

                newElement.transform.rotation = Quaternion.LookRotation(cameraForward, scanContext.origin.up); // align with camera up 
                Vector3 scale = newElement.transform.localScale;
                scale.x = 2f * dist * (float)Math.Tan(angleRadian / 2f);
                scale.y = scale.x * ratio; // scale the entire photo on height
                newElement.transform.localScale = scale;
            }

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
        debugText = msg;
        try
        {
            result = JsonConvert.DeserializeObject<CustomVisionResult>(msg);
            debugText += "\n message ID " + result.ID;
        } catch (Exception e)
        {
            debugText = "Error " + e.Message;
        }
        
    }
}