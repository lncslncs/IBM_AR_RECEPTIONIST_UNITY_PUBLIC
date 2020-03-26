// Request Microphone permission on android
// UCL COMP0016 Team 12 Jan 2020; Written by Lilly Neubauer
//referencing Unity Manual example script for Android Mic permission https://docs.unity3d.com/Manual/android-RequestingPermissions.html


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class MicrophonePermissions : MonoBehaviour
{

   // GameObject request_mic_permission_dialog
    void Start()
    {
        #if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) {
            Permission.RequestUserPermission(Permission.Microphone);
            //request_mic_permission_dialog = new GameObject();
        }
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
