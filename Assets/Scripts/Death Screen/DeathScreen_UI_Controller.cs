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

    // Global State
    private Global_State globalState;

    // UI Elements

    private Button retryBtn;
    private Button mainMenuBtn;

    void Awake(){
        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        globalState.Reset();

        retryBtn = uiDocument.rootVisualElement.Query<Button>("retry-btn");
        mainMenuBtn = uiDocument.rootVisualElement.Query<Button>("main-menu-btn");
       
        retryBtn.RegisterCallback<ClickEvent>(evt => globalState.LoadScene("Arena"));
        mainMenuBtn.RegisterCallback<ClickEvent>(evt => globalState.LoadScene("Main Menu"));
    }

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
    }

    void OnDisable(){
        controls.UI.Disable();
    }
}
