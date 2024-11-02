using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BuffManager {
    
    private static BuffManager _instance = null;
    private static readonly object Padlock = new object();
    private List<Buff> _appliedBuffs;
    private LuxController _target;
    
    public static BuffManager Instance {
        get {
            lock (Padlock) {
                _instance ??= new BuffManager();
                return _instance;
            }
        }
    }
    
    /// <summary>
    /// Initialize Buff Manager
    /// </summary>
    /// <param name="controller">The player controller the buffs will affect</param>
    public void Init(LuxController controller) {
        _target = controller;
        _appliedBuffs = new List<Buff>();
    }

    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {
        foreach (var buff in _appliedBuffs.ToList()) {
            buff.currentTimer -= Time.deltaTime;

            if (buff.currentTimer <= 0) {
                buff.currentTimer = 0;
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
            buff.currentTimer = buff.duration;
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