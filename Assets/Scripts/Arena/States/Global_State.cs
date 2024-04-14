using System.Collections.Generic;
using UnityEngine;

public class Global_State : MonoBehaviour
{

    public bool paused = false;
    public RoundManager roundManager;
    private List<Round> rounds = new List<Round>();

    void Awake(){

        rounds.Add(new Round(2f, 10, 10));
        rounds.Add(new Round(2f, 10, 10));
        rounds.Add(new Round(2f, 10, 10));
        rounds.Add(new Round(2f, 10, 10));
        rounds.Add(new Round(2f, 10, 10));
        roundManager = new RoundManager(rounds);
        
    }

    void Update(){
        if(roundManager.inProgress){
            roundManager.Update();
        }
    }

    public void Pause(){

        if(!paused){
            Time.timeScale = 0;
            paused = true;
        }
        else{
            Time.timeScale = 1;
            paused = false;
        }
    }

   


}
