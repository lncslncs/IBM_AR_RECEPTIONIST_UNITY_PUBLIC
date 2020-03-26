using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
//using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARSubsystems;
using System;
public class ARTracking : MonoBehaviour {

    public GameObject skeleton;

    private Vector3 cameraPos;

    void start() {

        cameraPos = transform.position;

    }

    void update() {

        skeleton.transform.LookAt(cameraPos);

    }
}