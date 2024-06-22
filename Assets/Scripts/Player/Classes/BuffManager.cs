using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffManager
{

    private List<Buff> appliedBuffs;
    public LuxController target;

    public BuffManager(LuxController controller){
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

    public bool HasBuffApplied(Buff buff){
        return appliedBuffs.Contains(buff);
    }
}
