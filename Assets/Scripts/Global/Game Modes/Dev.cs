using Multiplayer.UI;
using UnityEngine;

namespace Global.Game_Modes {
    public class Dev : GameMode{
        
        private readonly int _requiredPlayerCount = 1;        
        private readonly int _countdownTime = 3;

        public Dev() {
            GameModeType = Type.Multiplayer;
            MinimumRequiredPlayers = _requiredPlayerCount;
            Name = "Dev";
        }
        
        public override void Start() {
            GlobalState.Pause(true);
            GameView.OnStartGameModeCountdown += () => StartCountdown(this);
            CountdownTimer = _countdownTime;
        }

        public override void Update() {
            GameTimer += Time.deltaTime;
        }

        public override void FixedUpdate() {
            
        }

        public override void End() {
            GameView.OnStartGameModeCountdown -= () => StartCountdown(this);
        }

        public override void Reset() {
            CountdownTimer = _countdownTime;
            GameTimer = 0f;
        }
    }
}