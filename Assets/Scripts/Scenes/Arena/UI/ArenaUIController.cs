using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;
using CustomElements;
using Global.Game_Modes;

public delegate void StartGameModeCountdown();

public class ArenaUIController : MonoBehaviour
{
    public static event StartGameModeCountdown OnStartGameModeCountdown; 

    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // UI Elements
    private VisualElement _abilityBox;
    private Label _debugCurrentState;
    private VisualElement _timeCounterContainer;
    private Label _timeCounter;
    private VisualElement _pauseMenu;
    private CountdownTimerElement _countdownTimerElement; // New custom countdown element
    private GlobalState _globalState;
    private Arena arena;

    // Input System
    private Controls _controls;

    // Player Reference
    public LuxPlayerController player;
    
    void Awake(){
        _globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        _countdownTimerElement = uiDocument.rootVisualElement.Q<CountdownTimerElement>("countdown-timer");
    }

    void Start() {
        InitTimeCounter();  
        GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
        GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
        OnStartGameModeCountdown?.Invoke();
    }

    void OnEnable(){
        _controls = new Controls();
        _controls.UI.Enable();
        _controls.UI.Q.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.W.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.E.performed += _ => ActivateAbilityAnimation(player.LuxEAbility);
        _controls.UI.R.performed += _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.ESC.performed += _ => ShowPauseMenu();
    }

    void OnDisable(){
        _controls.UI.Q.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.W.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.E.performed -= _ => ActivateAbilityAnimation(player.LuxEAbility);
        _controls.UI.R.performed -= _ => ActivateAbilityAnimation(player.LuxQAbility);
        _controls.UI.ESC.performed -= _ => ShowPauseMenu();
        _controls.UI.Disable();
        GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText -= _countdownTimerElement.UpdateCountdown;
    }

    void ActivateAbilityAnimation(Ability ability){

        if(ability.OnCooldown()) return;

        string overlayElementName = ability.key.ToLower() + "-overlay";
        _abilityBox = uiDocument.rootVisualElement.Q<VisualElement>(overlayElementName);
        
        if (_abilityBox != null){
            _abilityBox.style.visibility = Visibility.Visible;
            if(!_abilityBox.ClassListContains("bar-transition")){
                _abilityBox.AddToClassList("bar-transition");
                _abilityBox.style.transitionDuration =  new List<TimeValue> {ability.maxCooldown};
                StartCoroutine(WaitForTransition(ability.maxCooldown, _abilityBox));
            }  
        }
    }

    void Update(){
        ShowDebugInfo();
    }

    void FixedUpdate(){
        UpdateTimeCounter();
    }
    
    void InitTimeCounter(){
        _timeCounter = uiDocument.rootVisualElement.Q<Label>("time-count");
        _timeCounterContainer = uiDocument.rootVisualElement.Q<VisualElement>("time-count-container");
        _timeCounterContainer.style.visibility = Visibility.Visible;
    }
    
    void UpdateTimeCounter(){
        string currentTime = GlobalState.GameModeManager.CurrentGameMode.GetGameTimer();
        _timeCounter.text = currentTime;
    }
    
    void ShowDebugInfo(){
        _debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-state");
        _debugCurrentState.text = "Current State: " + player.currentState;

        if (GlobalState.GameModeManager.CurrentGameMode is Arena arena) {
            _debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round");
            _debugCurrentState.text = "Round: " + arena.RoundManager.GetCurrentRoundString();

            _debugCurrentState = uiDocument.rootVisualElement.Q<Label>("debug-current-round-timer");
            _debugCurrentState.text = "Next round starts in " + arena.RoundManager.GetCurrentRoundTime();
        }
    }

     void ShowPauseMenu(){

        GlobalState.Pause(!GlobalState.Paused);
        _pauseMenu = uiDocument.rootVisualElement.Q<VisualElement>("pause-menu");

        if(GlobalState.Paused){
            _pauseMenu.style.visibility = Visibility.Visible;
        }
        else{
            _pauseMenu.style.visibility = Visibility.Hidden;
        }
    }

    IEnumerator WaitForTransition(float delayInSeconds, VisualElement box){
        yield return new WaitForSeconds(delayInSeconds); 
        _abilityBox.style.transitionDuration =  new List<TimeValue> {0};
        box.style.visibility = Visibility.Hidden;
        box.RemoveFromClassList("bar-transition");
    }
}

