using Multiplayer.UI;
using UnityEngine;

namespace Global.Game_Modes {
    
    //public delegate void OnUpdateCountdownText(int timer);
    public class Duel : GameMode {

        //public event OnUpdateCountdownText UpdateCountdownText;

        private readonly int _requiredPlayeCount = 2;        
        private readonly int _countdownTime = 5;
        
        public Duel() {
            GameModeType = Type.Multiplayer;
            MinimumRequiredPlayers = _requiredPlayeCount;
            Name = "Duel";
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