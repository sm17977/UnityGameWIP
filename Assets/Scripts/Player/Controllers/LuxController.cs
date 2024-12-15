using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType {Player, Bot};

public class LuxController : NetworkBehaviour {
    
    public PlayerType playerType;
    public Champion championSO;
    public Champion champion;
    public ClientBuffManager ClientBuffManager;
    public GlobalState globalState;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>();
    
    public void ProcessPlayerDeath(){
        if(playerType == PlayerType.Player && !GlobalState.IsMultiplayer){
            StartCoroutine(DelayLoadScene());
        }

        if (playerType == PlayerType.Player && GlobalState.IsMultiplayer) {
            //canMove.Value = false;
        }
    }
    
    IEnumerator DelayLoadScene() {
        yield return new WaitForSeconds(0.5f);
        globalState.LoadScene("Death Screen");
    }
}
