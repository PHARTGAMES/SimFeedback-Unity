# SimFeedback-Unity
Unity Library for use with SimFeedback

WIP

SimFeedbackUnityTelemetry contains the telemetry provider code. It also contains a release folder which you can copy into simfeedback.

provider/SFUTelemetryProviderConfig.xml allows you to customise the provider details/images.


SimFeedbackUnity contains the unity project, you can copy the contents of the assets folder into your own project.

Scenes/SampleScene.unity is an example scene, atm it's just a cube with the SFUExample.cs script on it. All it basically does is assign the transform to the SimFeedbackUnity instance and start serving packets over udp.


Both the version of SFUAPI.cs in SimFeedbackUnityTelemetry and the version of SFUAPI.cs in SimFeedbackUnity must match, if you change one update the other and recompile.


There's some work in progress graphs under SFUTelemetry/Graps menu option that allow you to view what is being sent to the provider.


This has not been tested with a motion simulator so the setup in the current profile may have axis reversed, etc...
