using System.Collections;
using Global.Game_Modes;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType {Player, Bot};
public delegate void OnNetworkSpawn();

public class LuxController : NetworkBehaviour {
    public static event OnNetworkSpawn NetworkSpawn;
    
    public PlayerType playerType;
    public Champion lux;
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

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            Debug.Log("Lux Controller OnNetworkSpawn");
            NetworkSpawn?.Invoke();
        }
    }
}
