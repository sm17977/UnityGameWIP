using UnityEngine;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{
    public bool die = false;
    private float timer;
    private float distance;
    private float speed;
    private float time;
    public VisualEffect vfx;
    public LuxPlayerController playerController;
    public GameObject player;

    void Start(){

        vfx = GetComponent<VisualEffect>();

        // Distance between player and enemy
        distance = Vector3.Distance( new Vector3(player.transform.position.x, 1f, player.transform.position.z), playerController.Lux_AI.transform.position);
        // Particle speed
        speed = 7;
        // Particle lifetime
        time = distance/speed;

        // Set VFX Graph exposed properties
        vfx.SetVector3("targetDirection", playerController.direction);
        vfx.SetFloat("lifetime", time);
        vfx.SetFloat("speed", speed);

        // Initiate timer
        timer = time;
    }

    void Update(){

        // Set die to true when the particle lifetime ends
        if(timer > 0){
            timer -= Time.deltaTime;
        }
        else{
            die = true;
        }
    }

}
