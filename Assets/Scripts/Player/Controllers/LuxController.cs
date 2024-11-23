using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType {Player, Bot};

public class LuxController : NetworkBehaviour {
    
    public PlayerType playerType;
    public Champion championSO;
    public Champion champion;
    public BuffManager BuffManager;
    public GlobalState globalState;
    public bool canMove = true;
    
    public void ProcessPlayerDeath(){
        if(playerType == PlayerType.Player && !GlobalState.IsMultiplayer){
            StartCoroutine(DelayLoadScene());
        }

        if (playerType == PlayerType.Player && GlobalState.IsMultiplayer) {
            canMove = false;
        }
    }
    
    IEnumerator DelayLoadScene() {
        yield return new WaitForSeconds(0.5f);
        globalState.LoadScene("Death Screen");
    }
}
