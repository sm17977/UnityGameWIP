namespace Global.Game_Modes {
    public abstract class GameMode {
        public string Name;
        public float GameTimer;
        public int CountdownTimer = 3;
        public bool CountdownActive = false;
    }

}