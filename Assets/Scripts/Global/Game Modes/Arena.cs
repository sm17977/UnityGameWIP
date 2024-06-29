using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Global.Game_Modes {
    public class Arena : GameMode {
        
        public readonly RoundManager RoundManager;
        public Arena(Ability ability) {
            
            var rounds = new List<Round>();

            rounds.Add(new Round(30f, 1f, 0.6f, ability));
            rounds.Add(new Round(30f, 1f, 0.45f, ability));
            rounds.Add(new Round(30f, 1f, 0.35f, ability));
            rounds.Add(new Round(30f, 1f, 0.25f, ability));
            rounds.Add(new Round(30f, 1f, 0.2f, ability));

            RoundManager = RoundManager.Instance;
            RoundManager.Init(rounds);
            
            GlobalState.Pause(true);
            InitCountdown();
            
        }
        private void InitCountdown(){
            CountdownTimer = 3;
            CountdownActive = false;
        }

        public void Update() {
            if(RoundManager.InProgress){
                GameTimer += Time.deltaTime;
                RoundManager.Update();
            }
        }
        
        public string GetGameTimer(){
            decimal decimalValue = System.Math.Round((decimal)GameTimer, 2);
            return decimalValue.ToString() + "s";
        }
        
        public void Reset(){
            InitCountdown();
        }
    }
}