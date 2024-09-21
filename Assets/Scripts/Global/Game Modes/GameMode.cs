using System.Collections;
using UnityEngine;

namespace Global.Game_Modes {
    public delegate void OnUpdateCountdownText(int timer);
    public delegate void OnHideCountdown();
    public delegate void OnShowCountdown();

    public abstract class GameMode {
        public event OnUpdateCountdownText UpdateCountdownText;
        public event OnHideCountdown HideCountdown;
        public event OnShowCountdown ShowCountdown;
        
        public string Name;
        public float GameTimer;
        public int CountdownTimer;
        public bool CountdownActive;
        public Type GameModeType;
        public int MinimumRequiredPlayers;
        public bool RespawnEnabled;
        
        public enum Type {
            SinglePlayer,
            Multiplayer
        }

        /// <summary>
        /// Start the gamemode
        /// </summary>
        public abstract void Start();
        
        /// <summary>
        /// Update the gamemode
        /// Used for timers or anything that needs to run in update
        /// </summary>
        public abstract void Update();
        
        /// <summary>
        /// Update the gamemode (fixed)
        /// Used for timers or anything that needs to run in fixed update
        /// </summary>
        public abstract void FixedUpdate();
        
        /// <summary>
        /// End the gamemode
        /// </summary>
        public abstract void End();
        
        /// <summary>
        /// Reset the gamemode
        /// Used to reset any values on gamemode end to ensure the gamemode starts correctly
        /// </summary>
        public abstract void Reset();
        
        /// <summary>
        /// Set a countdown timer in seconds for the start of the gamemode
        /// Sends event to UI scripts to update the countdown in the UI
        /// </summary>
        /// <param name="time">Duration of the gamemode countdown in seconds</param>
        public void SetCountdownTimer(int time) {
            CountdownTimer = time;
            UpdateCountdownText?.Invoke(time);
        }
        
        /// <summary>
        /// Get the current game time
        /// </summary>
        /// <returns>The current game time in seconds</returns>
        public string GetGameTimer(){
            decimal decimalValue = System.Math.Round((decimal)GameTimer, 2);
            return decimalValue + "s";
        }

        public void StartCountdown(GameMode gameMode) {
            if (!CountdownActive) {
                ShowCountdown?.Invoke();
                CountdownManager.Instance.StartCountdown(gameMode, OnCountdownComplete);
            }
        }
        
        public void OnCountdownComplete() {
            GlobalState.Pause(false);
            HideCountdown?.Invoke();
        }
    }
}