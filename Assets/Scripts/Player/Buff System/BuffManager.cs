using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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
            buff.CurrentTimer -= Time.deltaTime;

            if (buff.CurrentTimer <= 0) {
                buff.CurrentTimer = 0;
                if (!GlobalState.IsMultiplayer) {
                    buff.Clear(_target);
                    _appliedBuffs.Remove(buff);
                }
                else {
                    Debug.Log("CheckServerBuff");
                    CheckServerBuff(buff.Key);
                }
            }
        }
    }

    private void CheckServerBuff(string key) {
        var rpcController = _target.gameObject.GetComponent<RPCController>();
        var clientId = _target.NetworkObject.OwnerClientId;
        rpcController.CheckServerBuffRemovedRpc(key, clientId);
    }

    public void RemoveBuff(string key) {
        if (_appliedBuffs.Count == 0) return;
        Debug.Log("Buffs length before removing: " + _appliedBuffs.Count);
        foreach(var buff in _appliedBuffs) {
            if (buff.Key == key) {
                _appliedBuffs.Remove(buff);
                buff.Clear(_target);
                break;
            }
        }
        Debug.Log("Buffs length after removing: " + _appliedBuffs.Count);
    }
    
    /// <summary>
    /// Add a buff to the buff list
    /// </summary>
    /// <param name="buff">The buff to add</param>
    public void AddBuff(Buff buff) {
        Debug.Log("Buffs length before adding: " + _appliedBuffs.Count);
        if (!_appliedBuffs.Contains(buff)) {
            buff.CurrentTimer = buff.Duration;
            _appliedBuffs.Add(buff);
        }
        Debug.Log("Buffs after before adding: " + _appliedBuffs.Count);
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