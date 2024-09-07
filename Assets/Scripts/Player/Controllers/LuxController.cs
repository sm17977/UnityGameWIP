using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType {Player, Bot};


public class LuxController : NetworkBehaviour {
    
    public PlayerType playerType;
    public Champion lux;
    public BuffManager BuffManager;
    public GlobalState globalState;
    public bool canMove = true;


    public void ProcessPlayerDeath(){
        if(playerType == PlayerType.Player){
            StartCoroutine(DelayLoadScene());
        }
    }

    IEnumerator DelayLoadScene() {
        yield return new WaitForSeconds(0.5f);
        globalState.LoadScene("Death Screen");
    }

    public override void OnNetworkSpawn() {
        Debug.Log("OnNetworkSpawn");
    }
}
