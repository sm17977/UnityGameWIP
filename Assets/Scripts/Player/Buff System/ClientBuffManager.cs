using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ClientBuffManager {
    
    private List<Buff> _appliedBuffs;
    private LuxController _target;
    
    /// <summary>
    /// Initialize Buff Manager
    /// </summary>
    /// <param name="controller">The player controller the buffs will affect</param>
    public ClientBuffManager(LuxController controller) {
        _target = controller;
        _appliedBuffs = new List<Buff>();
    }

    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {

        double serverTimeNow = NetworkManager.Singleton.ServerTime.Time;
        
        foreach (var buff in _appliedBuffs.ToList()) {
            double timeLeft = buff.BuffEndTime - serverTimeNow;
            if (timeLeft <= 0) {
                buff.Clear(_target);
                _appliedBuffs.Remove(buff);
            }
        }
    }
    
    /// <summary>
    /// Add a buff to the buff list
    /// </summary>
    /// <param name="buff">The buff to add</param>
    public void AddBuff(Buff buff) {
        if (!_appliedBuffs.Contains(buff)) {
            _appliedBuffs.Add(buff);
        }
    }

    /// <summary>
    /// Check if buff has been applied
    /// </summary>
    /// <param name="buff">The buff to check</param>
    /// <returns>boolean</returns>
    public bool HasBuffApplied(Buff buff) {
        return _appliedBuffs.Contains(buff);
    }
}