using System;
using System.Collections;
using System.Collections.Generic;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.SceneManagement;
public delegate void NotifyGameModeType();

public class GlobalState : MonoBehaviour {
 
    public static bool IsMultiplayer;
    public static bool Paused;
    public static GameModeManager GameModeManager;

    public static List<GameMode> MultiplayerGameModes;
    
    public string currentScene;
    public Ability LuxQAbilitySO;
    public event NotifyGameModeType OnMultiplayerGameMode;
    public event NotifyGameModeType OnSinglePlayerGameMode;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        
        GameModeManager = GameModeManager.Instance;
        var ability = Instantiate(LuxQAbilitySO);
        GameModeManager.AddGameMode(new Arena(ability));
        GameModeManager.AddGameMode(new Duel());
        GameModeManager.AddGameMode(new MultiplayerPlaceholder());
        MultiplayerGameModes = GameModeManager.GameModes.FindAll(gm => gm.GameModeType == GameMode.Type.Multiplayer);
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        //SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void Start() {
 
    }

    private void Update() {
        if (GameModeManager.CurrentGameMode != null) {
            GameModeManager.Update();
        }
    }

    private void FixedUpdate() {
        if (GameModeManager.CurrentGameMode != null) {
            GameModeManager.FixedUpdate();
        }
    }

    private void OnSceneLoaded(Scene nextScene, LoadSceneMode mode) {
        currentScene = nextScene.name;

        switch (currentScene) {
            case "Arena":
                GameModeManager.ChangeGameMode("Arena");
                IsMultiplayer = false;
                OnSinglePlayerGameMode?.Invoke();
                
                break;

            case "Multiplayer":
                IsMultiplayer = true;
                OnMultiplayerGameMode?.Invoke();
                break;
        }
    }
    
    public void LoadScene(string sceneName) {
        if (!string.IsNullOrEmpty(sceneName)) {
            if (sceneName == "Exit") {
                Application.Quit();
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
        else {
            Debug.LogError("Scene name not found: " + sceneName);
        }
    }
    
    public static void Pause(bool shouldPause) {
        Time.timeScale = shouldPause ? 0 : 1;
        Paused = shouldPause;
    }
}