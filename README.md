# Hololens-ComputerVision


This project shows how to use windows [media capture](https://msdn.microsoft.com/library/windows/apps/windows.media.capture.mediacapture.aspx) in Hololens 2 to access the Camera, take picture send the image to a computer vision (CV) service and display an augmented reality asset close to where the picture was taken depending on the CV result.

It is inspired by previous work from Joost van Schaik available at https://localjoost.github.io/using-azure-custom-vision-object/

and the Picture Sample project I realized for Hololens2.



Refer to [Locatable camera info from Microsoft](https://docs.microsoft.com/en-us/windows/mixed-reality/locatable-camera) for details on the Device Camera.

[Locatable camera in Unity](https://docs.microsoft.com/en-us/windows/mixed-reality/locatable-camera-in-unity) provides the key API and sample scripts.


### Dependencies
- Unity 2019.4.23f1
- Text Mesh Pro
- MRTK Foundation 2.6.1
- JSON .net for Unity
- NuGet
- M2Mqtt from NuGet

### Install
Open the project in Unity 2019.4.23f1

Unity should ask to install Text Mesh Pro
- Text Mesh Pro via Window, Text Mesh Pro, install TMP Essential Resources
- install the MRTK Foundations / Standard Assets via Microsoft Mixed Reality Feature Tool (beta)
- MRTK Libraries via XR plugin management in Unity Project settings

Verify that ...
- your build settings is correctly set for Hololens 2 e.g. switch to Universal Windows Platform, ARM64
- The build contains the scene TakePictureScene (using Add Open Scenes)
- 'MixedRealitySpeechCommandsProfile' in CustomProfile folder contains the key word "Scan Image".

> Note: it could be needed to reinstall M2Mqtt Package via Unity Asset Manager.

Build, deploy to Hololens 2 (through Visual Studio 19).

The App should request access to Camera and Microphone, accept !

Enjoy taking Picture by just saying "Scan Image"

Setup MQTT topics to receive the image and send image recognition back


### Next Steps
- document ComputerVision part
