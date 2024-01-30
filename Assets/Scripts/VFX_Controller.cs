using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFX_Controller : MonoBehaviour
{
    public bool die = false;
    private float timer;
    private float distance;
    private float speed;
    private float time;
    public VisualEffect vfx;
    public Lux_Player_Controller playerController;
    public GameObject player;

    void Start(){

    
        vfx = GetComponent<VisualEffect>();
      
        Debug.Log("Lux AI Pos: " + playerController.Lux_AI.transform.position);

        distance = Vector3.Distance( new Vector3(player.transform.position.x, 1f, player.transform.position.z), new Vector3(-3f, 0.5f, 4.3f));
        speed = 7;
        time = distance/speed;

        Debug.Log("Distance: " + distance + " Speed: " + speed + " Time: " + time + " Direction: " + playerController.direction + " Position: " + playerController.transform.position);

        vfx.SetVector3("targetDirection", playerController.direction);
        vfx.SetFloat("lifetime", time);
        vfx.SetFloat("speed", speed);

        timer = time;
    }

    // Update is called once per frame
    void Update(){

        if(timer > 0){
            timer -= Time.deltaTime;
        }
        else{
            die = true;
        }
    }


}
