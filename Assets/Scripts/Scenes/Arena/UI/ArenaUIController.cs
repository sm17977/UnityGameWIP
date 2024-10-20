using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;
using CustomElements;
using Global.Game_Modes;
using QFSW.QC;

public delegate void StartArenaCountdown();

public class ArenaUIController : MonoBehaviour
{
    public static event StartDuelCountdown OnStartGameModeCountdown; 

    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // UI Elements
    private VisualElement _abilityBox;
    private Label _debugCurrentState;
    private VisualElement _timeCounterContainer;
    private Label _timeCounter;
    private VisualElement _pauseMenu;
    private CountdownTimerElement _countdownTimerElement;
    private VisualElement _healthBarContainer;
    
    // Input System
    private Controls _controls;
    
    // Health bar positioning offset
    private float _healthBarYOffset = -50f;

    // Player Reference
    public LuxPlayerController player;
    
    void Awake(){
        _countdownTimerElement = uiDocument.rootVisualElement.Q<CountdownTimerElement>("countdown-timer");
        _healthBarContainer = uiDocument.rootVisualElement.Q<VisualElement>("health-bar-container");
    }

    void Start() {
        InitTimeCounter();  
        GlobalState.GameModeManager.CurrentGameMode.UpdateCountdownText += _countdownTimerElement.UpdateCountdown;
        GlobalState.GameModeManager.CurrentGameMode.HideCountdown += _countdownTimerElement.HideCountdown;
        OnStartGameModeCountdown?.Invoke();
        SetHealthBarPosition();
        Debug.Log(_healthBarContainer.transform);
    }
    
    void OnDrawGizmos(){
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_healthBarContainer.transform.position, 0.3f);
    }

    void SetHealthBarPosition() {
       
        Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
            _healthBarContainer.panel, player.healthBarAnchor.transform.position, player.mainCamera);

        newPosition.x += -(Screen.width / 2);
        newPosition.y += -(Screen.height) + _healthBarYOffset;
        
        _healthBarContainer.transform.position = newPosition;
    }
    
    public Vector2 WithNewX(Vector2 vector, float newX) => new Vector2(newX, vector.y);

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

    private void LateUpdate() {
        SetHealthBarPosition();
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

