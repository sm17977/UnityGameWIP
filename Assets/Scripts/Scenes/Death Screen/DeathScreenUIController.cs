using System;
using Global.Game_Modes;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public class DeathScreenUIController : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Input System
    private Controls controls;

    // Global State
    private GlobalState globalState;

    // UI Elements

    private Button retryBtn;
    private Button mainMenuBtn;

    private Label roundReached;
    private Label timeSurvived;
    private Label apm;
    private Label accuracy;

    void Awake(){
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();

        roundReached = uiDocument.rootVisualElement.Q<Label>("round-reached-label");
        timeSurvived = uiDocument.rootVisualElement.Q<Label>("time-label");
        apm = uiDocument.rootVisualElement.Q<Label>("apm-label");
        accuracy = uiDocument.rootVisualElement.Q<Label>("accuracy-label");

        if (GlobalState.GameModeManager.CurrentGameMode is Arena arena) {
            roundReached.text = arena.RoundManager.GetCurrentRoundString();
            timeSurvived.text = arena.GetGameTimer();
        }

        retryBtn = uiDocument.rootVisualElement.Q<Button>("retry-btn");
        mainMenuBtn = uiDocument.rootVisualElement.Q<Button>("main-menu-btn");
       
        retryBtn.RegisterCallback<ClickEvent>(evt => LoadArena());
        mainMenuBtn.RegisterCallback<ClickEvent>(evt => LoadMainMenu());
    }

    private void LoadArena(){
        globalState.LoadScene("Arena");
    }

    private void LoadMainMenu(){
        globalState.LoadScene("Main Menu");
    } 

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
    }

    void OnDisable(){
        controls.UI.Disable();
    }
}
