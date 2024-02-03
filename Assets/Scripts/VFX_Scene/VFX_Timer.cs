using UnityEngine;
using UnityEngine.VFX;

public class VFX_Timer : MonoBehaviour
{
    public bool die = false;
    public float speed;
    private float distance;
    private float time;
    private float timer;
    private VisualEffect vfx;
    public VFX_Spawner spawner;


    // Start is called before the first frame update
    void Start()
    {
        vfx = GetComponent<VisualEffect>();

        // Distance between player and enemy
        distance = Vector3.Distance(spawner.transform.position, spawner.target.transform.position);
        // Particle speed
        speed = 7;
        // Particle lifetime
        time = distance/speed;

        // Set VFX Graph exposed properties
        vfx.SetVector3("targetDirection", spawner.targetDirection);
        vfx.SetFloat("lifetime", time);
        vfx.SetFloat("speed", speed);

        // Initiate timer
        timer = time;
        
    }

    // Update is called once per frame
    void Update()
    {
        // Set die to true when the particle lifetime ends
        if(timer > 0){
            timer -= Time.deltaTime;
        }
        else{
            die = true;
        }
    }
}
