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
    private Label debugCurrentRound;
    private Label debugCurrentRoundTimer;
    private VisualElement pauseMenu;

    // Input System
    private Controls controls;

    // Player Reference
    public Lux_Player_Controller player;

    // Global State
    public GameObject globalStateObj;
    private Global_State globalState;

 
    void OnEnable(){

        globalState = globalStateObj.GetComponent<Global_State>();

        controls = new Controls();
        controls.UI.Enable();
        controls.UI.Q.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.W.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.E.performed += _ => ActivateAbilityAnimation(player.LuxEAbility);
        controls.UI.R.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.ESC.performed += _ => ShowPauseMenu();
       
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
        ShowDebugInfo();
    }

    void ShowDebugInfo(){
        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-state");
        debugCurrentState.text = "Current State: " + player.currentState;
        
        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round");
        debugCurrentState.text = "Round: " + globalState.roundManager.GetCurrentRound();

        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round-timer");
        debugCurrentState.text = "Next round starts in " + globalState.roundManager.GetCurrentRoundTime();

    }

    void ShowPauseMenu(){

        globalState.Pause();
        pauseMenu = uiDocument.rootVisualElement.Q<VisualElement>("pause-menu");

        if(globalState.paused){
            pauseMenu.style.visibility = Visibility.Visible;
        }
        else{
            pauseMenu.style.visibility = Visibility.Hidden;
        }
    }


    IEnumerator WaitForTransition(float delayInSeconds, VisualElement box){
        yield return new WaitForSeconds(delayInSeconds); 
        abilityBox.style.transitionDuration =  new List<TimeValue> {0};
        box.style.visibility = Visibility.Hidden;
        box.RemoveFromClassList("bar-transition");
    }
  
}

