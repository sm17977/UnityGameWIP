using System.Collections.Generic;
using UnityEngine;

namespace Global.Game_Modes {
    public class GameModeManager {
        
        private static GameModeManager _instance = null;
        private static readonly object Padlock = new object();
        private List<GameMode> _gameModes;
        public GameMode CurrentGameMode;
        
        private GameModeManager() {
            _gameModes = new List<GameMode>();
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
            _gameModes.Add(gameMode);
        }
        
        public void ChangeGameMode(string gameModeName) {
            
            GameMode nextGameMode = _gameModes.Find(g => g.Name == gameModeName);

            if (nextGameMode == null) return;
            
            CurrentGameMode?.End();
            CurrentGameMode?.Reset();
            CurrentGameMode = nextGameMode;
            CurrentGameMode.Start();
        }

        public void Update() {
            CurrentGameMode.Update();
        }
    }
}