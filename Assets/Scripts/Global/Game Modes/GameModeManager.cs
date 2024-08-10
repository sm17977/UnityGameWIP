using System.Collections.Generic;
using UnityEngine;

namespace Global.Game_Modes {
    public class GameModeManager {
        
        private static GameModeManager _instance = null;
        private static readonly object Padlock = new object();
        public List<GameMode> GameModes;
        public GameMode CurrentGameMode;
        
        private GameModeManager() {
            GameModes = new List<GameMode>();
        }
        
        public static GameModeManager Instance {
            get {
                lock (Padlock) {
                    _instance ??= new GameModeManager();
                    return _instance;
                }
            }
        }

        public void AddGameMode(GameMode gameMode) {
            GameModes.Add(gameMode);
        }
        
        public void ChangeGameMode(string gameModeName) {
            
            GameMode nextGameMode = GameModes.Find(gm => gm.Name == gameModeName);

            if (nextGameMode == null) return;
            
            CurrentGameMode?.End();
            CurrentGameMode?.Reset();
            CurrentGameMode = nextGameMode;
            CurrentGameMode.Start();
        }

        public void Update() {
            CurrentGameMode.Update();
        }

        public void FixedUpdate() {
            CurrentGameMode.FixedUpdate();
        }
    }
}