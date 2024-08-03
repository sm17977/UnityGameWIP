using System.Collections.Generic;
using UnityEngine;

public sealed class RoundManager {

    private static RoundManager _instance = null;
    private static readonly object Padlock = new object();
    
    public bool InProgress = true;
    private int _currentRoundIndex;
    private Round _currentRound;
    private List<Round> _rounds;

    public static RoundManager Instance {
        get {
            lock (Padlock) {
                _instance ??= new RoundManager();
                return _instance;
            }
        }
    } 

    /// <summary>
    /// Initialize Round Manager
    /// </summary>
    /// <param name="rounds">A list of rounds</param>
    public void Init(List<Round> rounds){
        _rounds = rounds;
        _currentRoundIndex = 0;
        _currentRound = rounds[_currentRoundIndex];
        _currentRound.Start();
    }

    /// <summary>
    /// Update the round manager to handle round transitions
    /// </summary>
    public void Update(){
        if(!_currentRound.IsComplete()){
            _currentRound?.Execute();
        }
        else{
            ProgressToNextRound();
        }
    }

    /// <summary>
    /// Transition current round to next round
    /// </summary>
    private void ProgressToNextRound(){
        if(_currentRoundIndex < _rounds.Count - 1){
            _currentRoundIndex++;
            _currentRound = _rounds[_currentRoundIndex];
            _currentRound.Start();
        }
        else{
            InProgress = false;
        }
    }

    /// <summary>
    /// Get the current round as a string
    /// </summary>
    /// <returns>Current round as a string</returns>
    public string GetCurrentRoundString(){
        return (_currentRoundIndex + 1).ToString();
    }

    /// <summary>
    /// Get the current round
    /// </summary>
    /// <returns>Current round object</returns>
    public Round GetCurrentRound(){
        return _currentRound;
    }

    /// <summary>
    /// Get the time of the current round
    /// </summary>
    /// <returns>Current round time</returns>
    public float GetCurrentRoundTime(){
        return _currentRound.currentTime;
    }
}
