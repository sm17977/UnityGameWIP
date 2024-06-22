using System.Collections;
using System.Collections.Generic;
using Global.Game_Modes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalState : MonoBehaviour
{

    public static GlobalState Instance;
    public static bool Paused = false;
    public string currentScene;
    public GameMode CurrentGameMode;

    public Arena Arena;
    public Ability LuxQAbilitySO;
    
    void Awake(){
        if (Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else{
            Destroy(gameObject);
        }
    }

    void Start(){
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update(){
        if (CurrentGameMode != null && CurrentGameMode.GetType() == typeof(Arena)) {
            Arena.Update();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
        currentScene = scene.name;

        switch(currentScene){
            case "Arena": 
                var ability = Object.Instantiate(LuxQAbilitySO);
                Arena = new Arena(ability);
                CurrentGameMode = Arena;
                StartCoroutine(Countdown(Arena));
                break;

            case "Multiplayer":


                break;
        }
    }
    
    public static void Pause(bool shouldPause) {
        Time.timeScale = shouldPause ? 0 : 1;
        Paused = shouldPause;
    }
    
    public void LoadScene(string sceneName){

        if (!string.IsNullOrEmpty(sceneName)){
            if(sceneName == "Exit"){
                Application.Quit();
                return;
            }
            SceneManager.LoadScene(sceneName);
        }
        else{
            Debug.LogError("Scene name not found: " + sceneName);
        }
    }
    
    IEnumerator Countdown(GameMode gameMode) {
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
