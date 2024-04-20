using System.Collections.Generic;

public class RoundManager {

    public bool inProgress = true;
    private int currentRoundIndex;
    private Round currentRound;
    private List<Round> rounds;

    public RoundManager(List<Round> rounds){
        this.rounds = rounds;
        currentRoundIndex = 0;
        currentRound = rounds[currentRoundIndex];
        currentRound.Start();
    }

    public void Update(){
        if(!currentRound.IsComplete()){
            currentRound?.Execute();
        }
        else{
            ProgressToNextRound();
        }
    }

    public void ProgressToNextRound(){
        if(currentRoundIndex < rounds.Count - 1){
            currentRoundIndex++;
            currentRound = rounds[currentRoundIndex];
            currentRound.Start();
        }
        else{
            inProgress = false;
        }
    }

    public string GetCurrentRound(){
        return (currentRoundIndex + 1).ToString();
    }

    public Round GetCurrentRoundInstance(){
        return currentRound;
    }

    public float GetCurrentRoundTime(){
        return currentRound.currentTime;
    }

}
