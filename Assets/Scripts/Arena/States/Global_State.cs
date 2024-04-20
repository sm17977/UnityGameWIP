using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Global_State : MonoBehaviour
{
    public bool paused = false;
    public RoundManager roundManager;
    private List<Round> rounds = new List<Round>();
    public Ability LuxQAbilitySO;
    public Ability ability;
    public int countdownTimer = 3;
    public bool countdownActive;
   
    void Awake(){

        DontDestroyOnLoad(gameObject);

        ability = Object.Instantiate(LuxQAbilitySO);

        rounds.Add(new Round(10f, 1f, 0.6f, ability));
        rounds.Add(new Round(10f, 1f, 0.5f, ability));
        rounds.Add(new Round(10f, 1f, 0.4f, ability));
        rounds.Add(new Round(10f, 1f, 0.3f, ability));
        rounds.Add(new Round(30f, 1f, 0.2f, ability));

        roundManager = new RoundManager(rounds);
    }

    void Start(){
        InitCountdown();
    }

    void Update(){
        if(roundManager.inProgress){
            roundManager.Update();
        }
    }

    public void Pause(bool shouldPause) {
        Time.timeScale = shouldPause ? 0 : 1;
        paused = shouldPause;
    }

    public void InitCountdown(){
        Pause(true);
        StartCoroutine(Countdown());

    }

    IEnumerator Countdown() {
        countdownActive = true;
        while (countdownTimer > 0) {
            yield return new WaitForSecondsRealtime(1f);
            countdownTimer--;
        }

        countdownTimer = 0; 
        yield return new WaitForSecondsRealtime(1f); // Delay 1 sec to show "Go!" after countdown ends
        countdownActive = false;
        Pause(false); 
    }

    private void InitArena(){

        
    }

    public void LoadScene(string sceneName){

        if (!string.IsNullOrEmpty(sceneName)){
            if(sceneName == "Exit"){
                Application.Quit();
                return;
            }
            Debug.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else{
            Debug.LogError("Scene name not found: " + sceneName);
        }
    }
}
