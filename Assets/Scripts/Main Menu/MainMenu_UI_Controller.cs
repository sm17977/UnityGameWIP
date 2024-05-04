using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu_UI_Controller : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Input System
    private Controls controls;

    // Global State
    public Global_State globalState;


    void Awake(){

        Debug.Log("TEST");
        var mainMenuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();
      
        foreach (var button in mainMenuBtns){
            button.RegisterCallback<ClickEvent>(evt => globalState.LoadScene(button.text));
        }  
    }

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
    }

    void OnDisable(){
        controls.UI.Disable();
    }
}
