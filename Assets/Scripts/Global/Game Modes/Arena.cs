﻿using System.Collections.Generic;
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
            MinimumRequiredPlayers = 1;
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
            // Subscribe to events sent from the UI Controller to start the countdown
            ArenaUIController.OnStartGameModeCountdown += () => StartCountdown(this);
            CountdownTimer = _countdownTime;
        }
        
        public override void Update() {
            if(RoundManager.InProgress){
                GameTimer += Time.deltaTime;
                RoundManager.Update();
            }
        }
        public override void FixedUpdate() {
        }

        public override void End() {
            ArenaUIController.OnStartGameModeCountdown -= () => StartCountdown(this);
        }
        
        public override void Reset(){
            CountdownTimer = _countdownTime;
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
    }
}