

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnNetworkSpawn(){
        if(IsOwner && IsClient){
            SendToServerRpc("Message from client");
        }
    }

    void Move(){
        transform.position = new Vector3(1, 1, 1);
    }

    [Rpc(SendTo.Server)]
    public void SendToServerRpc(string msg) {
        if(IsServer){
            Debug.Log($"Player position on Server is {transform.position}");
            Move();
            Debug.Log($"Player moved on Server to position {transform.position}");
            SendToClientRpc("Message from server");
        }
    }

    [Rpc(SendTo.NotServer)]
    void SendToClientRpc(string msg){
        Debug.Log("In Client RPC, message sent from server: " + msg);
    }

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
