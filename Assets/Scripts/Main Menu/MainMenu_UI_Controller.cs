using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu_UI_Controller : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Input System
    private Controls controls;

    // UI Elements

    private VisualElement gamemode1_btn;
    private VisualElement exit_btn;


    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();

        var mainMenuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();
      
        foreach (var button in mainMenuBtns){
            button.RegisterCallback<ClickEvent>(evt => LoadScene(button.text));
        }        
    }

    void LoadScene(string sceneName){

        if (!string.IsNullOrEmpty(sceneName)){
            if(sceneName == "Exit"){
                Application.Quit();
                return;
            }
            Debug.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else{
            Debug.LogError("Scene name not found: " + sceneName);
        }
    }
}
