
using UnityEngine;

public class Round{

    private bool complete = false;
    private float duration;
    public float currentTime;
    private float maxProjectileCount;
    private float projectileFrequency;
    //public Ability ability;


    public Round(float duration, float maxProjectileCount, float projectileFrequency) {

        this.duration = duration;
        this.maxProjectileCount = maxProjectileCount;
        this.projectileFrequency = projectileFrequency;
        //this.ability = ability;

    }

    public void Start() {
        currentTime = duration;
    }

    public void Execute(){
        if(currentTime <= 0){
            End();
        }
        else{
            currentTime -= Time.deltaTime;
        }
    }

    public void End(){
        currentTime = 0;
        complete = true;
    }

    public bool IsComplete(){
        return complete;
    }


}
