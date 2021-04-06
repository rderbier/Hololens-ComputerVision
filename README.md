# Hololens-ComputerVision


This project shows how to use windows [media capture](https://msdn.microsoft.com/library/windows/apps/windows.media.capture.mediacapture.aspx) in Hololens 2 to access the Camera, take picture send the image to a computer vision (CV) service and display an augmented reality asset close to where the picture was taken depending on the CV result.

It is inspired by previous work from Joost van Schaik available at
https://localjoost.github.io/using-azure-custom-vision-object/

and the Picture Sample project I realized for Hololens2.

Using Unity 2919.4.f1, new XR plugin management and MRTK 2.6.1


Refer to [Locatable camera info from Microsoft](https://docs.microsoft.com/en-us/windows/mixed-reality/locatable-camera) for details on the Device Camera.

[Locatable camera in Unity](https://docs.microsoft.com/en-us/windows/mixed-reality/locatable-camera-in-unity) provides the key API and sample scripts.
### Dependencies
- MRTK Foundation 2.6.1
- Unity 2019.4.23f1
- use of XR plugin management in Unity Project settings

### Design
Image taken by Hololens Camera is 16:9 and 64.69 deg horizontal fov.
This is used to compute how to align the picture with the real word.
At 1m from the view point the image should be 1.26m wide to fit the reality.

As the image is large for image recognition, we decided to crop the thrid of it at the center.
Croping is done using Texture2D GetPixels and SetPixels.

Image is sent on a MQTT channel as a JPEG base64encoded string.

The ComputerVision result is received on different channel.



### Install
Open the project in Unity 2019.4.23f1



Unity should ask to install Text Mesh Pro and should install the MRTK Libraries

Verify that
- 'MixedRealitySpeechCommandsProfile' in CustomProfile folder contains the key word "Scan Image".
- your build settings is correctly set for Hololens 2
- The build contains the scene TakePictureScene

Build, deploy to Hololens 2 (through Visual Studio).

The App should request access to Camera and Microphone, accept !

Enjoy taking Picture by just saying "Take Picture"

### Next Steps
- Send the image to a Computer Vision service and display the result as an augmented reality asset close to where the picture was taken
