using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;

public class ArenaUIController : MonoBehaviour
{

    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // UI Elements
    private VisualElement abilityBox;
    private Label debugCurrentState;
    private Label countdownTimer;
    private VisualElement countdownContainer;
    private VisualElement timeCounterContainer;
    private Label timeCounter;
    private VisualElement pauseMenu;

    // Input System
    private Controls controls;

    // Player Reference
    public LuxPlayerController player;

    // Global State
    private static GlobalState globalState;

    void Awake(){
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
    }

    void Start() {
        InitCountdownTimer();
        InitTimeCounter();  
    }

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
        controls.UI.Q.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.W.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.E.performed += _ => ActivateAbilityAnimation(player.LuxEAbility);
        controls.UI.R.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.ESC.performed += _ => ShowPauseMenu();
    }

    void OnDisable(){
        controls.UI.Q.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.W.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.E.performed -= _ => ActivateAbilityAnimation(player.LuxEAbility);
        controls.UI.R.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        controls.UI.ESC.performed -= _ => ShowPauseMenu();
        controls.UI.Disable();
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
        UpdateCountdownTimer();
        ShowDebugInfo();
       
    }

    void FixedUpdate(){
        UpdateTimeCounter();
    }

    void UpdateTimeCounter(){
        string currentTime = globalState.Arena.GetGameTimer();
        timeCounter.text = currentTime;
    }

    void InitTimeCounter(){
        timeCounter = uiDocument.rootVisualElement.Q<Label>("time-count");
        timeCounterContainer = uiDocument.rootVisualElement.Q<VisualElement>("time-count-container");
        timeCounterContainer.style.visibility = Visibility.Visible;
    }

    void InitCountdownTimer(){
        countdownContainer = uiDocument.rootVisualElement.Q<VisualElement>("countdown-container");
        countdownTimer = uiDocument.rootVisualElement.Q<Label>("countdown-timer");
        countdownTimer.text = globalState.Arena.CountdownTimer.ToString();
        countdownContainer.style.visibility = Visibility.Visible;
    }

    void UpdateCountdownTimer() {
        if(globalState.Arena.CountdownActive){
            if (globalState.Arena.CountdownTimer >= 1) {
                countdownTimer.text = globalState.Arena.CountdownTimer.ToString();
            }
            else if (globalState.Arena.CountdownTimer == 0){
                countdownTimer.text = "Go!";
            }
        }
        else {
            countdownContainer.style.visibility = Visibility.Hidden;
        }
    }
    void ShowDebugInfo(){
        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-state");
        debugCurrentState.text = "Current State: " + player.currentState;
        
        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round");
        debugCurrentState.text = "Round: " + globalState.Arena.RoundManager.GetCurrentRoundString();

        debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round-timer");
        debugCurrentState.text = "Next round starts in " + globalState.Arena.RoundManager.GetCurrentRoundTime();
    }

     void ShowPauseMenu(){

        GlobalState.Pause(!GlobalState.Paused);
        pauseMenu = uiDocument.rootVisualElement.Q<VisualElement>("pause-menu");

        if(GlobalState.Paused){
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

