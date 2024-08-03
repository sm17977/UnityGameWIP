using System.Collections;
using System.Collections.Generic;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.SceneManagement;
public delegate void Notify();

public class GlobalState : MonoBehaviour {
 
    public static bool IsMultiplayer;
    public static bool Paused;
    public static GameModeManager GameModeManager;
    
    public static readonly List<string> MultiplayerGameModes = new() {
        "Duel",
        "Test1",
        "Test2",
        "Test3",
    };
    
    public string currentScene;
    public Ability LuxQAbilitySO;
    public event Notify OnMultiplayerGameMode;
    public event Notify OnSinglePlayerGameMode;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameModeManager = GameModeManager.Instance;
        var ability = Instantiate(LuxQAbilitySO);
        GameModeManager.AddGameMode(new Arena(ability));
        GameModeManager.AddGameMode(new Duel());
    }

    private void Update() {
        if (GameModeManager.CurrentGameMode != null) {
            GameModeManager.Update();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
        currentScene = scene.name;

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