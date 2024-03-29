using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Unity.VisualScripting;

public class UI_Controller : MonoBehaviour
{

    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // UI Elements
    private VisualElement abilityBox;
    private Label debugCurrentState;

    // Input System
    private Controls controls;

    // Player Reference
    public Lux_Player_Controller player;

 
    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();
        controls.UI.Q.performed += _ => ActivateAbilityAnimation("Q");
        controls.UI.W.performed += _ => ActivateAbilityAnimation("W");
        controls.UI.E.performed += _ => ActivateAbilityAnimation("E");
        controls.UI.R.performed += _ => ActivateAbilityAnimation("R");
       
    }

    void ActivateAbilityAnimation(string abilityKey){

        string overlayElementName = abilityKey.ToLower() + "-overlay";
        abilityBox = uiDocument.rootVisualElement.Q<VisualElement>(overlayElementName);
        
        if (abilityBox != null){
            abilityBox.style.visibility = Visibility.Visible;
            if(!abilityBox.ClassListContains("bar-transition")){
                abilityBox.AddToClassList("bar-transition");
                StartCoroutine(WaitForTransition(3f, abilityBox));
            }  
        }
    }

    void Update(){
        showDebugInfo();
    }

    void showDebugInfo(){
        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-state");
        debugCurrentState.text = "Current State: " + player.currentState;

    }

     IEnumerator WaitForTransition(float delayInSeconds, VisualElement box){
        yield return new WaitForSeconds(delayInSeconds); 
        box.style.visibility = Visibility.Hidden;
        box.RemoveFromClassList("bar-transition");
     }
  
}

