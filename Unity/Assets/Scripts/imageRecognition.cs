using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class imageRecognition : MonoBehaviour
{
    private Camera _camera;

    private ARTrackedImageManager _arTrackedImageManager;
    private ARTrackedImage _trackedImage;
    // Start is called before the first frame update
    void Start()
    {
        _camera = FindObjectOfType<Camera>();
    }

    void Awake() {
        _arTrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onEnable() {
        _arTrackedImageManager.trackedImagesChanged += OnImageChanged;
        
    }

    public void onDisable() {
        _arTrackedImageManager.trackedImagesChanged -= OnImageChanged;
    }

    public void OnImageChanged(ARTrackedImagesChangedEventArgs args) {
        //Debug.Log("AVATAR INSTANTIATED");
        foreach (var trackedImage in args.added) {
            Debug.Log(_arTrackedImageManager.name);
        }
    }
}
