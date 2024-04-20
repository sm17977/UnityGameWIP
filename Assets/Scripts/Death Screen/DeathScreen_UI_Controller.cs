using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class DeathScreen_UI_Controller : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Input System
    private Controls controls;

    // UI Elements

    // private VisualElement gamemode1_btn;
    // private VisualElement exit_btn;


    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();

    }
}
