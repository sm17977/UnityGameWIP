using System.Collections;
using UnityEngine;

public enum PlayerType {Player, Bot};
public class Lux_Controller : MonoBehaviour
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
