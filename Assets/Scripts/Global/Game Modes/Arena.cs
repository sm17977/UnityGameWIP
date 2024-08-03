using System.Collections.Generic;
using QFSW.QC;
using UnityEngine;

namespace Global.Game_Modes {
    public class Arena : GameMode {
        
        public readonly RoundManager RoundManager;
        private readonly int _countdownTime = 3;
        private Ability _ability;
        public Arena(Ability ability) {
            _ability = ability;
            GameModeType = Type.SinglePlayer;
            Name = "Arena";

            RoundManager = RoundManager.Instance;
            // Initialise multiple rounds and store them in a list
            var rounds = new List<Round>(){
                new (5f, 1f, 0.6f, ability),
                new (5f, 1f, 0.45f, ability),
                new (30f, 1f, 0.35f, ability),
                new (30f, 1f, 0.25f, ability),
                new (30f, 1f, 0.2f, ability),
            };
            RoundManager.Init(rounds);
        }

        public override void Start() {

            GlobalState.Pause(true);
            SetCountdownTimer(_countdownTime);
            CountdownManager.Instance.StartCountdown(this, OnCountdownComplete);
        }
        
        public override void Update() {
            if(RoundManager.InProgress){
                GameTimer += Time.deltaTime;
                RoundManager.Update();
            }
        }

        public override void End() {
        }
        
        public override void Reset(){
            SetCountdownTimer(_countdownTime);
            GameTimer = 0f;
            // Initialise multiple rounds and store them in a list
            var rounds = new List<Round>(){
                new (5f, 1f, 0.6f, _ability),
                new (5f, 1f, 0.45f, _ability),
                new (30f, 1f, 0.35f, _ability),
                new (30f, 1f, 0.25f, _ability),
                new (30f, 1f, 0.2f, _ability),
            };
            RoundManager.Init(rounds);
        }

        private void OnCountdownComplete() {
            GlobalState.Pause(false);
        }
    }
}