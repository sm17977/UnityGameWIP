using UnityEngine;

namespace Global.Game_Modes {
    
    public delegate void OnUpdateCountdownText(int timer);
    public class Duel : GameMode {

        public event OnUpdateCountdownText UpdateCountdownText;

        private readonly int _requiredPlayeCount = 2;        
        private readonly int _countdownTime = 5;
        
        public Duel() {
            GameModeType = Type.Multiplayer;
            MinimumRequiredPlayers = _requiredPlayeCount;
            Name = "Duel";
        }
        
        public override void Start() {
            SetCountdownTimer(_countdownTime);
            GlobalState.Pause(true);
            Debug.Log("START - Update Countdown Text: " + CountdownTimer);
            CountdownManager.Instance.StartCountdown(this, OnCountdownComplete);
        }

        public override void Update() {
            GameTimer += Time.deltaTime;
        }

        public override void FixedUpdate() {
        }
        
        public override void End() {
        }

        public override void Reset() {
            GameTimer = 0f;
            SetCountdownTimer(_countdownTime);
        }

        public override void SetCountdownTimer(int time) {
            CountdownTimer = time;
            UpdateCountdownText?.Invoke(time);
        }
        
        private void OnCountdownComplete() {
            GlobalState.Pause(false);
        }
    }
}