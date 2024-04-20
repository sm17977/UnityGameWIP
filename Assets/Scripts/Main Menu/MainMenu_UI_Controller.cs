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

    // UI Elements

    private VisualElement gamemode1_btn;
    private VisualElement exit_btn;


    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();

        var mainMenuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();
      
        foreach (var button in mainMenuBtns){
            button.RegisterCallback<ClickEvent>(evt => globalState.LoadScene(button.text));
        }        
    }


}
