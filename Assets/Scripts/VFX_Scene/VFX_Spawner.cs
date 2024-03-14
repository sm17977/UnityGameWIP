using UnityEngine;
using UnityEngine.VFX;

public class VFX_Spawner : MonoBehaviour{

    public VFX_Scene_Manager sceneManager;
    public GameObject vfx;
    public GameObject target;
    public Vector3 targetDirection;


    // Start is called before the first frame update
    void Start(){
        targetDirection = (target.transform.position - transform.position).normalized;
    }

    // Update is called once per frame
    void Update(){

        // Calculate direction from source to target
        targetDirection = (target.transform.position - transform.position).normalized;

        // Demo projectile on spacebar press
        if(Input.GetKeyDown(KeyCode.Space)){
           ShootProjectile();
        }

    }

    void ShootProjectile(){

        // Add the effect to the scene
        GameObject effect = Instantiate(vfx, transform.position, Quaternion.LookRotation(targetDirection, Vector3.up));
        VFX_Movement movement = effect.GetComponent<VFX_Movement>();
        movement.spawner = this;
        movement.direction = targetDirection;

        // Add effect to a list to handle gameobjects in the scene
        sceneManager.effectsList.Add(effect.gameObject);
    }
}
