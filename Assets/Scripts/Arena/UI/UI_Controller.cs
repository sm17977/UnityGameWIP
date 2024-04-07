using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

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
        controls.UI.Q.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.W.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.E.performed += _ => ActivateAbilityAnimation(player.LuxEAbility);
        controls.UI.R.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
       
    }

    void ActivateAbilityAnimation(Ability ability){

        if(ability.OnCooldown()) return;

        string overlayElementName = ability.key.ToLower() + "-overlay";
        abilityBox = uiDocument.rootVisualElement.Q<VisualElement>(overlayElementName);
        
        if (abilityBox != null){
            abilityBox.style.visibility = Visibility.Visible;
            if(!abilityBox.ClassListContains("bar-transition")){
                abilityBox.AddToClassList("bar-transition");
                abilityBox.style.transitionDuration =  new List<TimeValue> {ability.maxCooldown};
                StartCoroutine(WaitForTransition(ability.maxCooldown, abilityBox));
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
        abilityBox.style.transitionDuration =  new List<TimeValue> {0};
        box.style.visibility = Visibility.Hidden;
        box.RemoveFromClassList("bar-transition");
    }
  
}

