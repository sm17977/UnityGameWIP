using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox_Collision : MonoBehaviour
{

    private Lux_Player_Controller playerController;


    // Start is called before the first frame update
    void Start(){

        GameObject player = GameObject.Find("Lux_Player");
        playerController = player.GetComponent<Lux_Player_Controller>();
        
    }

    // Update is called once per frame
    void Update()
    {
   
    }

    void OnCollisionEnter(Collision collision){
        if(collision.gameObject.name == "Lux_AI"){
            playerController.projectiles.Remove(transform.parent.gameObject);
            Destroy(transform.parent.gameObject);
        }
    }
}
