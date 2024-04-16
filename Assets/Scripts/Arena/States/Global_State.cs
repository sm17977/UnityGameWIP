using System.Collections.Generic;
using UnityEngine;

public class Global_State : MonoBehaviour
{
    public bool paused = false;
    public RoundManager roundManager;
    private List<Round> rounds = new List<Round>();
    public Ability LuxQAbilitySO;
    public Ability ability;


    void Awake(){

        ability = Object.Instantiate(LuxQAbilitySO);

        rounds.Add(new Round(10f, 1f, 2f, ability));
        rounds.Add(new Round(10f, 1f, 1.6f, ability));
        rounds.Add(new Round(10f, 1f, 1.2f, ability));
        rounds.Add(new Round(10f, 1f, 1.2f, ability));
        rounds.Add(new Round(30f, 1f, 1.2f, ability));

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
