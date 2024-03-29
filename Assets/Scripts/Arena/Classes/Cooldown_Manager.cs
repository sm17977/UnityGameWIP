using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cooldown_Manager : MonoBehaviour
{
    
    public static Cooldown_Manager instance;
    private List<Ability> abilitiesOnCooldown = new List<Ability>();

    void Awake(){
        if(instance == null){
            instance = this;
        }
        else if(instance != this){
            Destroy(this);
        }
    }

    void Update(){

        foreach(Ability ability in abilitiesOnCooldown.ToList()){

            ability.currentCooldown -= Time.deltaTime;

            if(ability.currentCooldown <= 0){
                ability.currentCooldown = 0;
                abilitiesOnCooldown.Remove(ability);
            }
        }
    }

    public void StartCooldown(Ability ability){

        if(!abilitiesOnCooldown.Contains(ability)){
            ability.currentCooldown = ability.maxCooldown;
            abilitiesOnCooldown.Add(ability);
        }
    }
}
