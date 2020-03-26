// This script manages the logic of the main UI elements in the scene
// These UI elements appear on startup and are implemented seperately from UI elements that are attached to the avatar
// UCL COMP0016 Team 12 Jan 2020; Written by Lilly Neubauer

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIElementsLogic : MonoBehaviour
{

    public GameObject helpButton;

    public GameObject infoButton;

    public GameObject helpPanel;
    
    public GameObject infoPanel;

    public Button menuButton;

    public Button humanButton;

    public bool humanRequested;

    private bool menuVisible;

    private Button helpBtn;
    private Button infoBtn;

    private bool panelShowing;
    private bool menuElementsExpanded;

    void Start()
    {
        //Setup UI elements
        helpButton.SetActive(true);
        infoButton.SetActive(true);
        menuElementsExpanded = true;

        // BUG: Could not get menu to toggle so left it open.
        //menuButton.onClick.AddListener(delegate {toggleMenu();});

        helpBtn = helpButton.GetComponent(typeof(Button)) as Button;
        helpBtn.onClick.AddListener(delegate {showPanel(helpPanel);});

        infoBtn = infoButton.GetComponent(typeof(Button)) as Button;
        infoBtn.onClick.AddListener(delegate {showPanel(infoPanel);});

        humanButton.onClick.AddListener(requestHuman);
        
    }

    void Update()
    {
        // Check if a human has been requested by the avatar
        //if (!GameObject.Find("GPReceptionistAvatar").GetComponent<NewWatson>().this_humanRequested)
        //{
        //    humanRequested = false;
        //};
    }
    
    void requestHuman() {
        humanRequested = true;
    }

    void showPanel(GameObject panel) {
        if (panelShowing) {
            hidePanels();
        }
        panel.SetActive(true);
        panelShowing = true;
    }

    void hidePanels() {
        helpPanel.SetActive(false);
        infoPanel.SetActive(false);
        panelShowing = false;
    }

    void toggleMenu() {

        if (!menuElementsExpanded) {

        helpButton.SetActive(true);
        infoButton.SetActive(true);

        menuElementsExpanded = true;

        } else {

        helpButton.SetActive(false);
        infoButton.SetActive(false);

        menuElementsExpanded = false;

        }

    }

}
