// This script is run when the avatar is instantiated after the user points the camera at the trigger image
// UCL COMP0016 Team 12 Jan 2020; Written by Lilly Neubauer

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class rotateToFaceCamera : MonoBehaviour
{
    GameObject backAwayWarning; 

    GameObject moveCloserWarning;
    private Camera _camera;
    private Vector3 player;
    GameObject triggerImageUserPrompt;
    private bool avatarActive;


    
    // run when the avatar is instantiated
    void Start()
    {
        Debug.Log("AVATAR INSTANTIATED");
        // Turn off the user prompt that says "point camera at trigger image"
        triggerImageUserPrompt = GameObject.Find("PointPrompt");
        triggerImageUserPrompt.SetActive(false);

        // find the location of the camera i.e. the mobile device/user
        _camera = FindObjectOfType<Camera>();

        backAwayWarning = GameObject.Find("BackAwayPrompt");
        backAwayWarning.SetActive(false);

        moveCloserWarning = GameObject.Find("MoveCloserPrompt");
        moveCloserWarning.SetActive(false);
        
    }

    // Update is called once per frame
    void Update()
    {

        // isolate the y value of the transform of the object
        Vector3 playerPosition = new Vector3(_camera.transform.position.x,
                                           transform.position.y,
                                           _camera.transform.position.z );
        
        
       // rotate to face the camera, without y direction included
        this.transform.LookAt(playerPosition);

    // Get the distance of the player from the avatar
        float playerDistance = Vector3.Distance(this.transform.position, _camera.transform.position);
        //Debug.Log("The distance between player and camera is: " + playerDistance);

        if (playerDistance < 0.25) {
            Debug.Log("Player is too close");
            backAwayWarning.SetActive(true);
        } else if (playerDistance > 1) {
            backAwayWarning.SetActive(false);
            moveCloserWarning.SetActive(true);
        }
        else {
            backAwayWarning.SetActive(false);
            moveCloserWarning.SetActive(false);
        }
    }
}
