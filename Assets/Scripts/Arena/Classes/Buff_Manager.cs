using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Buff_Manager
{

    private List<Buff> appliedBuffs;
    public Lux_Controller target;

    public Buff_Manager(Lux_Controller controller){
        target = controller;
        appliedBuffs = new List<Buff>();
    }

    public void Update(){

        foreach(Buff buff in appliedBuffs.ToList()){

            buff.currentTimer -= Time.deltaTime;

            if(buff.currentTimer <= 0){
                buff.currentTimer = 0;
                buff.Clear(target);
                appliedBuffs.Remove(buff);
            }
        }
    }
    public void AddBuff(Buff buff){

        if(!appliedBuffs.Contains(buff)){
            buff.currentTimer = buff.duration;
            appliedBuffs.Add(buff);
        }
    }
}
