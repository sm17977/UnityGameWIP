using System;
using System.Collections;
using Global.Game_Modes;
using UnityEngine;

public class CountdownManager : MonoBehaviour {
    public static CountdownManager Instance;
    
    private void Awake() {
        if(Instance == null){
            Instance = this;
        }
        else if(Instance != this){
            Destroy(this);
        }
    }

    public void StartCountdown(GameMode gameMode, Action onCountdownComplete) {
        StartCoroutine(CountdownCoroutine(gameMode, onCountdownComplete));
    }

    private IEnumerator CountdownCoroutine(GameMode gameMode, Action onCountdownComplete) {
        gameMode.CountdownActive = true;

        while (gameMode.CountdownTimer > 0) {

            if (gameMode is Duel duel) {
                duel.SetCountdownTimer(gameMode.CountdownTimer);
            }
            
            yield return new WaitForSecondsRealtime(1f);
            gameMode.CountdownTimer--;
        }
        
        if (gameMode is Duel duelModeFinal) {
            duelModeFinal.SetCountdownTimer(0); 
        }

        gameMode.CountdownTimer = 0;
        yield return new WaitForSecondsRealtime(1f); // Delay 1 sec to show "Go!" after countdown ends
        gameMode.CountdownActive = false;
        GlobalState.Pause(false);
        onCountdownComplete?.Invoke();
    }
}