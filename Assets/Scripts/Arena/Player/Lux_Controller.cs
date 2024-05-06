

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

    public float timer = 3f;
    public bool canStartTimer = false;
    public bool sentRpc = false;
    public bool deffoCanMove = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
    }

    // Update is called once per frame
    public void Update(){

        if(timer > 0 && canStartTimer){
            timer -= Time.deltaTime;
        }

        if(timer <= 0){
            if(IsOwner && !sentRpc){
                SendToServerRpc("Message from client");
                sentRpc = true;
            }
        }
    }

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();
        canStartTimer = true;
    }

    void Move(){
        transform.position = transform.position + new Vector3(1, 0, 1);
        Physics.SyncTransforms();  
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SendToServerRpc(string msg) {
        Move();
        SendToClientRpc("Message from server");
    }

    [Rpc(SendTo.NotServer)]
    void SendToClientRpc(string msg){
        Debug.Log($"Player position on Client is {transform.position}, instance ID: {this.gameObject.GetInstanceID()}, network ID: {NetworkObjectId}");
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
