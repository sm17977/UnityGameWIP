using System.Collections;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.SceneManagement;
public delegate void Notify();

public class GlobalState : MonoBehaviour {
 
    public static bool IsMultiplayer = false;
    public static bool Paused = false;
    
    public string currentScene;
    public GameMode CurrentGameMode;
    public Arena Arena;
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
    }

    private void Update() {
        if (CurrentGameMode != null && CurrentGameMode.GetType() == typeof(Arena)) Arena.Update();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
        currentScene = scene.name;

        switch (currentScene) {
            case "Arena":
                IsMultiplayer = false;
                OnSinglePlayerGameMode?.Invoke();
                var ability = Instantiate(LuxQAbilitySO);
                Arena = new Arena(ability);
                CurrentGameMode = Arena;
                StartCoroutine(Countdown(Arena));
                break;

            case "Multiplayer":
                IsMultiplayer = true;
                OnMultiplayerGameMode?.Invoke();
                break;
        }
    }

    public static void Pause(bool shouldPause) {
        Time.timeScale = shouldPause ? 0 : 1;
        Paused = shouldPause;
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

    private IEnumerator Countdown(GameMode gameMode) {
        gameMode.CountdownActive = true;

        while (gameMode.CountdownTimer > 0) {
            yield return new WaitForSecondsRealtime(1f);
            gameMode.CountdownTimer--;
        }

        gameMode.CountdownTimer = 0;
        yield return new WaitForSecondsRealtime(1f); // Delay 1 sec to show "Go!" after countdown ends
        gameMode.CountdownActive = false;
        Pause(false);
    }
}