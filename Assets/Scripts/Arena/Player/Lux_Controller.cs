

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum PlayerType {Player, Bot};
public class Lux_Controller : NetworkBehaviour
{

    public PlayerType playerType;
    public Champion lux;
    public Buff_Manager buffManager;
    public Global_State globalState;
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

}
