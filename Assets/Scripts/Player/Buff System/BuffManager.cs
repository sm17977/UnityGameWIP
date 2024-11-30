using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public sealed class BuffManager {
    
    private List<Buff> _appliedBuffs;
    private LuxController _target;
    private HashSet<string> _pendingRemovalKeys = new HashSet<string>();
    
    /// <summary>
    /// Initialize Buff Manager
    /// </summary>
    /// <param name="controller">The player controller the buffs will affect</param>
    public BuffManager(LuxController controller) {
        _target = controller;
        _appliedBuffs = new List<Buff>();
    }

    /// <summary>
    /// Update the buff manager to handle buff duration
    /// </summary>
    public void Update() {
        foreach (var buff in _appliedBuffs.ToList()) {
            if (buff.CurrentTimer <= 0) {
                buff.CurrentTimer = 0;
                if (!GlobalState.IsMultiplayer) {
                    buff.Clear(_target);
                    _appliedBuffs.Remove(buff);
                }
                else {
                    CheckServerBuff(buff.Key);
                }
            }
            buff.CurrentTimer -= Time.deltaTime;
            Debug.Log("(Update) Buff Timer: " + buff.CurrentTimer);
        }
    }

    private void CheckServerBuff(string key) {
        if (_pendingRemovalKeys.Contains(key)) return;
        _pendingRemovalKeys.Add(key);
        
        var rpcController = _target.gameObject.GetComponent<RPCController>();
        var clientId = _target.NetworkObject.OwnerClientId;
        rpcController.CheckServerBuffRemovedRpc(key, clientId);
    }

    public void RemoveBuff(string key) {
        if (_appliedBuffs.Count == 0) return;
        foreach(var buff in _appliedBuffs) {
            if (buff.Key == key) {
                buff.Clear(_target);
                _appliedBuffs.Remove(buff);
                _pendingRemovalKeys.Remove(key);
                break;
            }
        }
    }
    
    /// <summary>
    /// Add a buff to the buff list
    /// </summary>
    /// <param name="buff">The buff to add</param>
    public void AddBuff(Buff buff) {
        if (!_appliedBuffs.Contains(buff)) {
            Debug.Log("Buff Duration: " + buff.Duration);
            buff.CurrentTimer = buff.Duration;
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