
using UnityEngine;
using UnityEngine.VFX;

public class VFX_Movement : MonoBehaviour
{
    public bool die = false;
    public float speed;
    private float distance;
    private float distanceTravelled = 0f;
    private float time;
    private float timer;
    private bool canMove = true;
    private VisualEffect vfx;
    private VisualEffect childVfx;
    private VisualEffect[] childVfxArray;
    public VFX_Spawner spawner;
    public Vector3 direction;


    // Start is called before the first frame update
    void Start()
    {
        vfx = GetComponent<VisualEffect>();
        childVfxArray = GetComponentsInChildren<VisualEffect>();
        int count = 0;
        foreach(VisualEffect vfx in childVfxArray){
            Debug.Log("VFX Child: " + vfx.gameObject.name + " Index: " + count);
            count++;
        }
        

        //childVfx = childVfxArray[1];
        //childVfx.Play();
        
        // Distance between player and enemy
         if(spawner != null){
            distance = Vector3.Distance(spawner.transform.position, spawner.target.transform.position);
        }

        // Particle lifetime
        time = distance/speed;

        vfx.SetFloat("lifetime", time);

        // Initiate timer
        timer = time;
        
    }

    // Update is called once per frame
    void Update()
    {

        if(spawner != null){
            distance = Vector3.Distance(transform.position, spawner.target.transform.position);
            distanceTravelled = Vector3.Distance(spawner.transform.position, transform.position);
        }
        
        if(distance > 0 && canMove){
            transform.Translate(speed * Time.deltaTime * direction, Space.World);
        }

        if( 1 > distanceTravelled && distanceTravelled < 3){
            
        }

        // Set die to true when the particle lifetime ends
        if(timer > 0){
            timer -= Time.deltaTime;
        }
        else{
            canMove = false;
            die = true;
            vfx.SetBool("showParticle", false);
            if(childVfx){
                childVfx.SetBool("showParticle", false);
            }
        }
    }


}
