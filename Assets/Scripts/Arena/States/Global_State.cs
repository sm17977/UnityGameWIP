using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Global_State : MonoBehaviour
{
    public bool paused = false;
    public RoundManager roundManager;
    private List<Round> rounds;
    public Ability LuxQAbilitySO;
    public Ability ability;
    public int countdownTimer;
    public bool countdownActive;
    private string currentScene;
    private bool initArena;
    private float gameTimer;
   
    void Awake(){
        DontDestroyOnLoad(gameObject);
    }

    void Start(){
        gameTimer = 0f;
        initArena  = false;
        countdownTimer = 3;
        rounds = new List<Round>();
        InitArena();
    }

    void Update(){

        currentScene = SceneManager.GetActiveScene().name;

        if(currentScene == "Arena"){
            
            if(!initArena){
                InitCountdown();
                initArena = true;
            }

            if(roundManager.inProgress){
                gameTimer += Time.deltaTime;
                roundManager.Update();
            }
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
        ability = Object.Instantiate(LuxQAbilitySO);

        rounds.Add(new Round(30f, 1f, 0.6f, ability));
        rounds.Add(new Round(30f, 1f, 0.45f, ability));
        rounds.Add(new Round(30f, 1f, 0.35f, ability));
        rounds.Add(new Round(30f, 1f, 0.25f, ability));
        rounds.Add(new Round(30f, 1f, 0.2f, ability));

        roundManager = new RoundManager(rounds);
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

    public string GetGameTimer(){
        decimal decimalValue = System.Math.Round((decimal)gameTimer, 2);
        return decimalValue.ToString() + "s";
    }

    public void Reset(){
        Start();
    }
}
